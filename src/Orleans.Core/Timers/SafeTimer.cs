using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Orleans.Runtime
{
    /// <summary>
    /// SafeTimer - A wrapper class around .NET Timer objects, with some additional built-in safeguards against edge-case errors.
    /// 
    /// SafeTimer is a replacement for .NET Timer objects, and removes some of the more infrequently used method overloads for simplification.
    /// SafeTimer provides centralization of various "guard code" previously added in various places for handling edge-case fault conditions.
    /// 
    /// Log levels used: Recovered faults => Warning, Per-Timer operations => Verbose, Per-tick operations => Verbose3
    /// </summary>
    internal class SafeTimer : SafeTimerBase
    {
        private readonly TimerCallback syncCallbackFunc;
        private readonly object state;

        private SafeTimer(ILogger logger, TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period) : base(logger, dueTime, period)
        {
            this.syncCallbackFunc = callback;
            this.state = state;
        }

        public static SafeTimer StartNew(ILogger logger, TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            var result = new SafeTimer(logger, callback, state, dueTime, period);
            result.Start(dueTime, period);
            return result;
        }

        public override string Name => "Orleans.Runtime.SafeTimer";

        protected override void HandleTimerCallback()
        {
            if (!this.IsValid) return;

            try
            {
                if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTrace((int)ErrorCode.TimerBeforeCallback, "About to make sync timer callback for timer {Timer}", this.Name);
                syncCallbackFunc(state);
                if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTrace((int)ErrorCode.TimerAfterCallback, "Completed sync timer callback for timer {Tomer}", this.Name);
            }
            catch (Exception exc)
            {
                Logger.LogWarning((int)ErrorCode.TimerCallbackError, exc, "Ignored exception {Exception} during sync timer callback {Timer}", exc, this.Name);
            }
            finally
            {
                // Queue next timer callback
                QueueNextTimerTick();
            }
        }
    }
}
