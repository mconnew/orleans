using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Orleans.Runtime
{
    internal class AsyncTaskSafeTimer : SafeTimerBase
    {
        private readonly Func<object, Task> asyncTaskCallback;
        private readonly object state;

        private AsyncTaskSafeTimer(ILogger logger, Func<object, Task> asyncTaskCallback, object state, TimeSpan dueTime, TimeSpan period) : base(logger, dueTime, period)
        {
            this.state = state;
            this.asyncTaskCallback = asyncTaskCallback;
        }

        public static AsyncTaskSafeTimer StartNew(ILogger logger, Func<object, Task> asyncTaskCallback, object state, TimeSpan dueTime, TimeSpan period)
        {
            var result = new AsyncTaskSafeTimer(logger, asyncTaskCallback, state, dueTime, period);
            result.Start(dueTime, period);
            return result;
        }

        public override string Name => "Orleans.Runtime.AsyncTaskSafeTimer";

        protected override void HandleTimerCallback()
        {
            if (!this.IsValid) return;
            _ = HandleTimerCallbackAsync();
        }

        private async Task HandleTimerCallbackAsync()
        {
            // There is a subtle race/issue here w.r.t unobserved promises.
            // It may happen than the asyncCallbackFunc will resolve some promises on which the higher level application code is depends upon
            // and this promise's await or CW will fire before the below code (after await or Finally) even runs.
            // In the unit test case this may lead to the situation where unit test has finished, but p1 or p2 or p3 have not been observed yet.
            // To properly fix this we may use a mutex/monitor to delay execution of asyncCallbackFunc until all CWs and Finally in the code below were scheduled 
            // (not until CW lambda was run, but just until CW function itself executed). 
            // This however will relay on scheduler executing these in separate threads to prevent deadlock, so needs to be done carefully. 
            // In particular, need to make sure we execute asyncCallbackFunc in another thread (so use StartNew instead of ExecuteWithSafeTryCatch).

            try
            {
                if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTrace((int)ErrorCode.TimerBeforeCallback, "About to make async task timer callback for timer {Timer}", this.Name);
                await asyncTaskCallback(state);
                if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTrace((int)ErrorCode.TimerAfterCallback, "Completed async task timer callback for timer {Timer}", this.Name);
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
