using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Threading;
using ExecutionContext = Orleans.Threading.ExecutionContext;

namespace Orleans.Runtime.Scheduler
{
    internal class OrleansSchedulerAsynchAgent : AsynchQueueAgent<IWorkItem>
    {
        private readonly ThreadPoolExecutorOptions.BuilderConfigurator configureExecutorOptionsBuilder;
        
        public OrleansSchedulerAsynchAgent(
            string name,
            ExecutorService executorService,
            int maxDegreeOfParalelism, 
            TimeSpan delayWarningThreshold, 
            TimeSpan turnWarningLengthThreshold,
            bool drainAfterCancel,
            ILoggerFactory loggerFactory) : base(name, executorService, loggerFactory)
        {
            configureExecutorOptionsBuilder = builder => builder
                .WithDegreeOfParallelism(maxDegreeOfParalelism)
                .WithDrainAfterCancel(drainAfterCancel)
                .WithWorkItemExecutionTimeTreshold(turnWarningLengthThreshold)
                .WithDelayWarningThreshold(delayWarningThreshold)
                .WithWorkItemStatusProvider(GetWorkItemStatus)
                .WithExceptionFilters(
                    new OuterExceptionHandler(Log),
                    new InnerExceptionHandler(Log));
        }

        protected override void Process(IWorkItem request)
        {
            RuntimeContext.InitializeThread();
            try
            {
                RuntimeContext.SetExecutionContext(request.SchedulingContext);
                request.Execute();
            }
            finally
            {
                RuntimeContext.ResetExecutionContext();
            }
        }
        
        protected override ThreadPoolExecutorOptions.Builder ExecutorOptionsBuilder => configureExecutorOptionsBuilder(base.ExecutorOptionsBuilder);

        private string GetWorkItemStatus(object item, bool detailed)
        {
            if (!detailed) return string.Empty;
            return item is WorkItemGroup group ? string.Format("WorkItemGroup Details: {0}", group.DumpStatus()) : string.Empty;
        }

        private sealed class OuterExceptionHandler : ExecutionExceptionFilter
        {
            private readonly ILogger log;

            public OuterExceptionHandler(ILogger log)
            {
                this.log = log;
            }

            public override bool ExceptionHandler(Exception ex, ExecutionContext context)
            {
                if (ex is ThreadAbortException)
                {
                    if (log.IsEnabled(LogLevel.Debug)) log.Debug("Received thread abort exception - exiting. {0}.", ex);
                    Thread.ResetAbort();
                }
                else
                {
                    log.Error(ErrorCode.Runtime_Error_100031, "Exception bubbled up to worker thread", ex);
                }

                context.CancellationTokenSource.Cancel();
                return true;
            }
        }

        private sealed class InnerExceptionHandler : ExecutionExceptionFilter
        {
            private readonly ILogger log;

            public InnerExceptionHandler(ILogger log)
            {
                this.log = log;
            }

            public override bool ExceptionHandler(Exception ex, ExecutionContext context)
            {
                if (ex is ThreadAbortException tae)
                {
                    // The current turn was aborted (indicated by the exception state being set to true).
                    // In this case, we just reset the abort so that life continues. No need to do anything else.
                    if (tae.ExceptionState != null && tae.ExceptionState.Equals(true))
                    {
                        Thread.ResetAbort();
                    }
                    else if (!context.CancellationTokenSource.IsCancellationRequested)
                    {
                        log.Error(ErrorCode.Runtime_Error_100029, "Caught thread abort exception, allowing it to propagate outwards.", ex);
                    }
                }
                else
                {
                    log.Error(ErrorCode.Runtime_Error_100030, 
                        string.Format("Thread caught an exception thrown from task {0}", context.WorkItem.State), ex);
                }

                return true;
            }
        }
    }
}
