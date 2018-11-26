using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HWT;
using Orleans.Timers;

namespace Orleans
{
    /*public static class TimerManager
    {
        private static readonly HashedWheelTimer Manager = new HashedWheelTimer(TimeSpan.FromMilliseconds(25), 1024, 0);

        public static async Task Delay(TimeSpan delay)
        {
            var awaitable = new TimerCallback();
            Manager.NewTimeout(awaitable, delay);
            await awaitable;
        }

        private class TimerCallback : TimerTask
        {
            private readonly TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public void Run(HWT.Timeout timeout) => this.completion.TrySetResult(true);

            public TaskAwaiter<bool> GetAwaiter() => this.completion.Task.GetAwaiter();
        }
    }*/

    public abstract class SimpleBatchWorker
    {
        private int status;

        private static class Status
        {
            public const int Idle = 0;
            public const int Running = 1;
            public const int Loop = 2;
        }

        /// <summary>Implement this member in derived classes to define what constitutes a work cycle</summary>
        protected abstract Task Work();

        /// <summary>
        /// Notify the worker that there is more work.
        /// </summary>
        public void Notify()
        {
            // If already running, loop, otherwise start running.
            if (Interlocked.CompareExchange(ref this.status, Status.Loop, Status.Running) == Status.Idle)
            {
                this.Run().Ignore();
            }
        }

        public void Notify(DateTime dueTime) => this.RunAfterDelay(dueTime).Ignore();

        private async Task RunAfterDelay(DateTime dueTime)
        {
            var delay = dueTime - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                await TimerManager.Delay(delay);
            }

            // If already running, loop, otherwise start running.
            if (Interlocked.CompareExchange(ref this.status, Status.Loop, Status.Running) == Status.Idle)
            {
                await this.Run();
            }
        }
        
        private async Task Run()
        {
            // If already running/looping, exit.
            if (Interlocked.CompareExchange(ref this.status, Status.Running, Status.Idle) != Status.Idle) return;

            var loopIterations = 0;
            do
            {
                // If we were told to loop, reset the loop status.
                Interlocked.CompareExchange(ref this.status, Status.Running, Status.Loop);

                try
                {
                    await this.Work();
                }
                catch
                {
                    // Ignore
                }

                if (loopIterations > 3)
                {
                    loopIterations = 0;
                    await TimerManager.Delay(TimeSpan.FromSeconds(1));
                }
            } while (Interlocked.CompareExchange(ref this.status, Status.Idle, Status.Running) == Status.Loop);
        }
    }

    /// <summary>
    /// General pattern for an asynchronous worker that performs a work task, when notified,
    /// to service queued work. Each work cycle handles ALL the queued work. 
    /// If new work arrives during a work cycle, another cycle is scheduled. 
    /// The worker never executes more than one instance of the work cycle at a time, 
    /// and consumes no resources when idle. It uses TaskScheduler.Current 
    /// to schedule the work cycles.
    /// </summary>
    public abstract class BatchWorker
    {
        private readonly object lockable = new object();

        private bool startingCurrentWorkCycle;

        private DateTime? scheduledNotify;

        // Task for the current work cycle, or null if idle
        private volatile Task currentWorkCycle;

        // Flag is set to indicate that more work has arrived during execution of the task
        private volatile bool moreWork;

        // Used to communicate the task for the next work cycle to waiters.
        // This value is non-null only if there are waiters.
        private TaskCompletionSource<Task> nextWorkCyclePromise;
        
        /// <summary>Implement this member in derived classes to define what constitutes a work cycle</summary>
        protected abstract Task Work();

        /// <summary>
        /// Notify the worker that there is more work.
        /// </summary>
        public void Notify()
        {
            lock (lockable)
            {
                if (currentWorkCycle != null || startingCurrentWorkCycle)
                {
                    // lets the current work cycle know that there is more work
                    moreWork = true;
                }
                else
                {
                    // start a work cycle
                    Start();
                }
            }
        }

        /// <summary>
        /// Instructs the batch worker to run again to check for work, if
        /// it has not run again already by then, at specified <paramref name="utcTime"/>.
        /// </summary>
        /// <param name="utcTime"></param>
        public void Notify(DateTime utcTime)
        {
            var now = DateTime.UtcNow;

            if (now >= utcTime)
            {
                Notify();
            }
            else
            {
                lock (lockable)
                {
                    if (!scheduledNotify.HasValue || scheduledNotify.Value > utcTime)
                    {
                        scheduledNotify = utcTime;

                        ScheduleNotify(utcTime, now).Ignore();
                    }
                }
            }
        }

        private Task ScheduleNotify(DateTime time, DateTime now)
        {
            if (scheduledNotify == now)
            {
                Notify();
            }

            return Task.CompletedTask;
        }

        private void Start()
        {
            // Indicate that we are starting the worker (to prevent double-starts)
            startingCurrentWorkCycle = true;

            // Clear any scheduled runs
            scheduledNotify = null;

            try
            {
                // Start the task that is doing the work
                currentWorkCycle = Work();
            }
            finally
            {
                // By now we have started, and stored the task in currentWorkCycle
                startingCurrentWorkCycle = false;

                // chain a continuation that checks for more work, on the same scheduler
                currentWorkCycle.ContinueWith(t => this.CheckForMoreWork(), TaskScheduler.Current);
            }
        }

        /// <summary>
        /// Executes at the end of each work cycle on the same task scheduler.
        /// </summary>
        private void CheckForMoreWork()
        {
            TaskCompletionSource<Task> signal = null;
            Task taskToSignal = null;

            lock (lockable)
            {
                if (moreWork)
                {
                    moreWork = false;

                    // see if someone created a promise for waiting for the next work cycle
                    // if so, take it and remove it
                    signal = this.nextWorkCyclePromise;
                    this.nextWorkCyclePromise = null;

                    // start the next work cycle
                    Start();

                    // the current cycle is what we need to signal
                    taskToSignal = currentWorkCycle;
                }
                else
                {
                    currentWorkCycle = null;
                }
            }

            // to be safe, must do the signalling out here so it is not under the lock
            signal?.SetResult(taskToSignal);
        }

        /// <summary>
        /// Check if this worker is idle.
        /// </summary>
        public bool IsIdle()
        {
            // no lock needed for reading volatile field
            return currentWorkCycle == null;
        }

        /// <summary>
        /// Wait for the current work cycle, and also the next work cycle if there is currently unserviced work.
        /// </summary>
        /// <returns></returns>
        public async Task WaitForCurrentWorkToBeServiced()
        {
            Task<Task> waitfortasktask = null;
            Task waitfortask = null;

            // Figure out exactly what we need to wait for
            lock (lockable)
            {
                if (!moreWork)
                {
                    // Just wait for current work cycle
                    waitfortask = currentWorkCycle;
                }
                else
                {
                    // we need to wait for the next work cycle
                    // but that task does not exist yet, so we use a promise that signals when the next work cycle is launched
                    if (nextWorkCyclePromise == null)
                    {
                        nextWorkCyclePromise = new TaskCompletionSource<Task>();
                    }

                    waitfortasktask = nextWorkCyclePromise.Task;
                }
            }

            // Do the actual waiting outside of the lock
            if (waitfortasktask != null)
            {
                await await waitfortasktask;
            }
            else if (waitfortask != null)
            {
                await waitfortask;
            }
        }

        /// <summary>
        /// Notify the worker that there is more work, and wait for the current work cycle, and also the next work cycle if there is currently unserviced work.
        /// </summary>
        public async Task NotifyAndWaitForWorkToBeServiced()
        {
            Task<Task> waitForTaskTask = null;
            Task waitForTask = null;

            lock (lockable)
            {
                if (currentWorkCycle != null || startingCurrentWorkCycle)
                {
                    moreWork = true;
                    if (nextWorkCyclePromise == null)
                    {
                        nextWorkCyclePromise = new TaskCompletionSource<Task>();
                    }

                    waitForTaskTask = nextWorkCyclePromise.Task;
                }
                else
                {
                    Start();
                    waitForTask = currentWorkCycle;
                }
            }

            if (waitForTaskTask != null)
            {
                await await waitForTaskTask;
            }
            else if (waitForTask != null)
            {
                await waitForTask;
            }
        }
    }

    public class BatchWorkerFromDelegate : BatchWorker
    {
        private readonly Func<Task> work;

        public BatchWorkerFromDelegate(Func<Task> work)
        {
            this.work = work;
        }

        protected override Task Work()
        {
            return work();
        }
    }

    public class SimpleBatchWorkerFromDelegate : SimpleBatchWorker
    {
        private readonly Func<Task> work;

        public SimpleBatchWorkerFromDelegate(Func<Task> work)
        {
            this.work = work;
        }

        protected override Task Work() => this.work();
    }
}