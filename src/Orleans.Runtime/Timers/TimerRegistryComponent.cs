using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Runtime.Scheduler;
using Orleans.Timers;

namespace Orleans.Runtime
{
    internal class TimerRegistryComponent : ITimerRegistryComponent
    {
        private readonly HashSet<IGrainTimer> _timers = new HashSet<IGrainTimer>();
        private readonly ILogger _logger;
        private readonly OrleansTaskScheduler _scheduler;

        public TimerRegistryComponent(ILogger<GrainTimer> logger, OrleansTaskScheduler scheduler)
        {
            _logger = logger;
            _scheduler = scheduler;
        }

        public IDisposable RegisterTimer(Func<object, Task> asyncCallback, object state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = GrainTimer.FromTaskCallback(_scheduler, _logger, asyncCallback, state, dueTime, period, registry: this);
            this.OnTimerCreated(timer);
            timer.Start();
            return timer;
        }

        public void OnTimerCreated(IGrainTimer timer)
        {
            AddTimer(timer);
        }

        internal void AddTimer(IGrainTimer timer)
        {
            lock (this)
            {
                _timers.Add(timer);
            }
        }

        public void StopAllTimers()
        {
            lock (this)
            {
                foreach (var timer in _timers)
                {
                    timer.Stop();
                }
            }
        }

        public void OnTimerDisposed(IGrainTimer orleansTimerInsideGrain)
        {
            lock (this) // need to lock since dispose can be called on finalizer thread, outside grain context (not single threaded).
            {
                _timers.Remove(orleansTimerInsideGrain);
            }
        }

        public Task WaitForAllTimersToFinish()
        {
            lock (this)
            {
                if (_timers == null)
                {
                    return Task.CompletedTask;
                }

                var tasks = new List<Task>();
                var timerCopy = _timers.ToList(); // need to copy since OnTimerDisposed will change the timers set.
                foreach (var timer in timerCopy)
                {
                    // first call dispose, then wait to finish.
                    Utils.SafeExecute(timer.Dispose, _logger, "timer.Dispose has thrown");
                    tasks.Add(timer.GetCurrentlyExecutingTickTask());
                }

                return Task.WhenAll(tasks);
            }
        }

    }
}
