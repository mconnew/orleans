using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Runtime.Scheduler;

namespace Orleans.Runtime
{
    internal class GrainTimer : IGrainTimer
    {
        private static readonly Func<object, Task> TimerTickCallback = state => ((GrainTimer)state).TimerTick();

        private readonly object state;
        private readonly TimeSpan dueTime;
        private readonly TimeSpan timerFrequency;
        private readonly ILogger logger;
        private readonly OrleansTaskScheduler scheduler;
        private readonly IGrainContext context;

        private Func<object, Task> asyncCallback;
        private AsyncTaskSafeTimer timer;
        private Task currentlyExecutingTickTask;
        
        private GrainTimer(OrleansTaskScheduler scheduler, IGrainContext context, ILogger logger, Func<object, Task> asyncCallback, object state, TimeSpan dueTime, TimeSpan period, string name)
        {
            this.context = context;
            this.scheduler = scheduler;
            this.state = state;
            this.logger = logger;
            this.Name = name;
            this.asyncCallback = asyncCallback;
            this.timer = new AsyncTaskSafeTimer(logger, TimerTickCallback, this);
            this.dueTime = dueTime;
            this.timerFrequency = period;
        }

        public string Name { get; }

        private bool TimerAlreadyStopped => timer == null || asyncCallback == null;

        internal static IGrainTimer FromTaskCallback(
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
            return result;
        }

        public void Start()
        {
            if (TimerAlreadyStopped)
                throw new ObjectDisposedException(String.Format("The timer {0} was already disposed.", GetFullName()));

            timer.Start(dueTime, timerFrequency);
        }

        public void Stop()
        {
            asyncCallback = null;
        }

        private async Task TimerTick()
        {
            if (TimerAlreadyStopped)
                return;
            try
            {
                // Schedule call back to grain context
                await this.scheduler.QueueNamedTask(() => ForwardToAsyncCallback(state), context, this.Name);
            }
            catch (InvalidSchedulingContextException exc)
            {
                logger.Error(ErrorCode.Timer_InvalidContext,
                    string.Format("Caught an InvalidSchedulingContextException on timer {0}, context is {1}. Going to dispose this timer!",
                        GetFullName(), context), exc);
                DisposeTimer();
            }
        }

        private async Task ForwardToAsyncCallback(object state)
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Maybe called by finalizer thread with disposing=false. As per guidelines, in such a case do not touch other objects.
        // Dispose() may be called multiple times
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                DisposeTimer();
            
            asyncCallback = null;
        }

        private void DisposeTimer()
        {
            timer?.Dispose();
            if (timer is null) return;
            timer = null;
            asyncCallback = null;
            (context as IActivationData)?.OnTimerDisposed(this);
        }
    }
}
