using System;


namespace Orleans.Runtime.Scheduler
{
    internal interface IWorkItem
#if NETCOREAPP
        : System.Threading.IThreadPoolWorkItem
#endif
    {
        string Name { get; }
        WorkItemType ItemType { get; }
        ISchedulingContext SchedulingContext { get; set; }
        TimeSpan TimeSinceQueued { get; }
        DateTime TimeQueued { get; set;  }
        bool IsSystemPriority { get; }
#if !NETCOREAPP
        void Execute();
#endif
    }
}
