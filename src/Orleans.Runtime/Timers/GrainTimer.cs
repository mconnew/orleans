using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Runtime.Scheduler;

namespace Orleans.Runtime
{
    internal class GrainTimer : SafeTimerBase, IGrainTimer
    {
        private static readonly Func<object, Task> ExecuteTickTask = timer => ((GrainTimer)timer).ForwardToAsyncCallback();
        private readonly object state;
        private readonly TimeSpan dueTime;
        private readonly TimeSpan timerFrequency;
        private readonly ILogger logger;
        private readonly IGrainContext context;
        private readonly TaskScheduler taskScheduler;

        private Func<object, Task> asyncCallback;
        private Task currentlyExecutingTickTask;
        
        private GrainTimer(
            OrleansTaskScheduler scheduler,
            IGrainContext context,
            ILogger logger,
            Func<object, Task> asyncCallback,
            object state,
            TimeSpan dueTime,
            TimeSpan period,
            string name)
            : base(logger)
        {
            if (scheduler is null) throw new ArgumentNullException(nameof(scheduler));
            this.context = context;
            this.state = state;
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.Name = name ?? "GrainTimer";
            this.asyncCallback = asyncCallback ?? throw new ArgumentNullException(nameof(asyncCallback));
            this.dueTime = dueTime;
            this.timerFrequency = period;
            this.taskScheduler = scheduler.GetTaskSchedulerOrDefault(context);
        }

        private bool TimerAlreadyStopped => asyncCallback is null || !this.IsValid;

        public override string Name { get; }

        internal static IGrainTimer StartNew(
            OrleansTaskScheduler scheduler,
            ILogger logger,
            Func<object, Task> asyncCallback,
            object state,
            TimeSpan dueTime,
            TimeSpan period,
            string name,
            IGrainContext context = null)
        {
            scheduler.CheckSchedulingContextValidity(context ?? RuntimeContext.CurrentGrainContext);
            var result = new GrainTimer(scheduler, context, logger, asyncCallback, state, dueTime, period, name);
            (context as IActivationData)?.OnTimerCreated(result);
            result.Start();
            return result;
        }

        public void Start()
        {
            if (TimerAlreadyStopped)
                throw new ObjectDisposedException(String.Format("The timer {0} was already disposed.", GetFullName()));

            base.Start(dueTime, timerFrequency);
        }

        public void Stop()
        {
            asyncCallback = null;
        }
        
        private async Task ForwardToAsyncCallback()
        {
            // AsyncSafeTimer ensures that calls to this method are serialized.
            var callback = asyncCallback;
            if (TimerAlreadyStopped) return;

            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace((int)ErrorCode.TimerBeforeCallback, "About to make timer callback for timer {Timer}", GetFullName());
            }

            try
            {
                RequestContext.Clear(); // Clear any previous RC, so it does not leak into this call by mistake. 
                currentlyExecutingTickTask = callback(state);
                await currentlyExecutingTickTask;

                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace((int)ErrorCode.TimerAfterCallback, "Completed timer callback for timer {Timer}", GetFullName());
                }
            }
            catch (Exception exc)
            {
                logger.LogError( 
                    (int)ErrorCode.Timer_GrainTimerCallbackError,
                    exc,
                    "Caught and ignored exception thrown from timer callback {Timer}: {Exception}",
                    GetFullName(),
                    exc);       
            }
            finally
            {
                currentlyExecutingTickTask = null;
                // if this is not a repeating timer, then we can
                // dispose of the timer.
                if (timerFrequency == Constants.INFINITE_TIMESPAN)
                {
                    DisposeTimer();
                }
            }
        }

        public Task GetCurrentlyExecutingTickTask()
        {
            return currentlyExecutingTickTask ?? Task.CompletedTask;
        }

        private string GetFullName()
        {
            var callback = asyncCallback;
            var callbackTarget = callback?.Target?.ToString() ?? string.Empty; 
            var callbackMethodInfo = callback?.GetMethodInfo()?.ToString() ?? string.Empty;
            return $"GrainTimer.{this.Name ?? string.Empty} TimerCallbackHandler:{callbackTarget ?? string.Empty}->{callbackMethodInfo ?? string.Empty}";
        }

        protected override void DisposeTimer()
        {
            if (!this.IsValid) return;
            base.DisposeTimer();
            (context as IActivationData)?.OnTimerDisposed(this);
        }

        protected override void HandleTimerCallback()
        {
            _ = HandleTimerCallbackAsync();
        }

        private async Task HandleTimerCallbackAsync()
        {
            if (TimerAlreadyStopped)
            {
                return;
            }

            try
            {
                // Schedule call back to grain context
                await Task.Factory.StartNew(ExecuteTickTask, this, CancellationToken.None, TaskCreationOptions.RunContinuationsAsynchronously, this.taskScheduler).Unwrap();
            }
            catch (InvalidSchedulingContextException exc)
            {
                logger.LogError(
                    (int)ErrorCode.Timer_InvalidContext,
                    exc,
                    "Caught an InvalidSchedulingContextException on timer {Timer}, context is {Context}: {Exception}",
                    GetFullName(),
                    context,
                    exc);
                DisposeTimer();
            }
            catch (Exception exc)
            {
                Logger.LogWarning((int)ErrorCode.TimerCallbackError, exc, "Ignored exception {Exception} during async task timer {Timer}", exc, this.Name);
            }
            finally
            {
                // Queue next timer callback
                QueueNextTimerTick();
            }
        }
    }
}
