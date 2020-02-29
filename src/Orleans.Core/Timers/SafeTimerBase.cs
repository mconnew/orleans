using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Orleans.Runtime
{
    /// <summary>
    /// SafeTimerBase - an internal base class for implementing sync and async timers in Orleans.
    /// </summary>
    internal abstract class SafeTimerBase : IDisposable
    {
        private static readonly TimerCallback TimerCallback = obj => ((SafeTimerBase)obj).HandleTimerCallback();
        private Timer               timer;
        private TimeSpan            timerFrequency;
        private bool                timerStarted;

        internal SafeTimerBase(ILogger logger)
        {
            this.Logger = logger;
            Init(logger, Constants.INFINITE_TIMESPAN, Constants.INFINITE_TIMESPAN);
        }

        internal SafeTimerBase(ILogger logger, TimeSpan dueTime, TimeSpan period)
        {
            this.Logger = logger;
            Init(logger, dueTime, period);
        }

        public abstract string Name { get; }

        protected ILogger Logger { get; }

        protected bool IsValid => this.timer is object;

        public void Start(TimeSpan due, TimeSpan period)
        {
            if (timerStarted) throw new InvalidOperationException(String.Format("Calling start on timer {0} is not allowed, since it was already created in a started mode with specified due.", Name));
            if (period == TimeSpan.Zero) throw new ArgumentOutOfRangeException("period", period, "Cannot use TimeSpan.Zero for timer period");
           
            timerFrequency = period;
            timerStarted = true;
            timer.Change(due, Constants.INFINITE_TIMESPAN);
        }

        private void Init(ILogger logger, TimeSpan due, TimeSpan period)
        {
            if (period == TimeSpan.Zero) throw new ArgumentOutOfRangeException("period", period, "Cannot use TimeSpan.Zero for timer period");

            timerFrequency = period;
            if (logger.IsEnabled(LogLevel.Debug)) logger.Debug(ErrorCode.TimerChanging, "Creating timer {0} with dueTime={1} period={2}", Name, due, period);

            timer = NonCapturingTimer.Create(TimerCallback, this, Constants.INFINITE_TIMESPAN, Constants.INFINITE_TIMESPAN);
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Maybe called by finalizer thread with disposing=false. As per guidelines, in such a case do not touch other objects.
        // Dispose() may be called multiple times
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeTimer();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        protected virtual void DisposeTimer()
        {
            if (timer != null)
            {
                try
                {
                    var t = timer;
                    timer = null;
                    if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebug((int)ErrorCode.TimerDisposing, "Disposing timer {Timer}", Name);
                    t.Dispose();

                }
                catch (Exception exc)
                {
                    Logger.LogWarning(
                        (int)ErrorCode.TimerDisposeError,
                        exc,
                        "Ignored error disposing timer {Timer}: {Exception}",
                        Name,
                        exc);
                }
            }
        }

        protected abstract void HandleTimerCallback();
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        protected void QueueNextTimerTick()
        {
            try
            {
                if (timer == null) return;
                
                if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTrace((int)ErrorCode.TimerChanging, "About to QueueNextTimerTick for timer {Timer}", Name);

                if (timerFrequency == Constants.INFINITE_TIMESPAN)
                {
                    //timer.Change(Constants.INFINITE_TIMESPAN, Constants.INFINITE_TIMESPAN);
                    DisposeTimer();

                    if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTrace((int)ErrorCode.TimerStopped, "Timer {Timer} is now stopped and disposed", Name);
                }
                else
                {
                    timer.Change(timerFrequency, Constants.INFINITE_TIMESPAN);

                    if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTrace((int)ErrorCode.TimerNextTick, "Queued next tick for timer {Timer} in {DueTimer}", Name, timerFrequency);
                }
            }
            catch (ObjectDisposedException ode)
            {
                Logger.LogWarning(
                    (int)ErrorCode.TimerDisposeError,
                    ode,
                    "Timer {Timer} already disposed - will not queue next timer tick", Name);
            }
            catch (Exception exc)
            {
                Logger.LogError(
                    (int)ErrorCode.TimerQueueTickError,
                    exc,
                    "Error queueing next timer tick - WARNING: timer {Timer} is now stopped: {Exception}",
                    Name,
                    exc);
            }
        }
    }
}
