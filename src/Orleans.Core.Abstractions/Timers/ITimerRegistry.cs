using System;
using System.Threading.Tasks;
using Orleans.Runtime;

namespace Orleans.Timers
{
    internal interface ITimerRegistryComponent
    {
        IDisposable RegisterTimer(Func<object, Task> asyncCallback, object state, TimeSpan dueTime, TimeSpan period);
        void StopAllTimers();
        void OnTimerDisposed(IGrainTimer timer);
        Task WaitForAllTimersToFinish();
    }
}