using System;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace Orleans.Runtime
{
    [EventSource(Name = "Microsoft-Orleans-CallBackData")]
    internal sealed class OrleansCallBackDataEvent : EventSource
    {
        public static readonly OrleansCallBackDataEvent Log = new OrleansCallBackDataEvent();

        [NonEvent]
        public void OnTimeout(Message message)
        {
            if (IsEnabled() && message.TraceContext is TraceContext traceContext)
            {
                OnTimeout(traceContext.ActivityId);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(1, Level = EventLevel.Warning)]
        private void OnTimeout(Guid relatedActivityId)
        {
            WriteEventWithRelatedActivityId(1, relatedActivityId);
        }
        
        [NonEvent]
        public void OnTargetSiloFail(Message message)
        {
            if (IsEnabled() && message.TraceContext is TraceContext traceContext)
            {
                OnTargetSiloFail(traceContext.ActivityId);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(2, Level = EventLevel.Warning)]
        private void OnTargetSiloFail(Guid relatedActivityId)
        {
            WriteEventWithRelatedActivityId(2, relatedActivityId);
        }

        [NonEvent]
        public void DoCallback(Message message)
        {
            if (IsEnabled() && message.TraceContext is TraceContext traceContext)
            {
                DoCallback(traceContext.ActivityId);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(3)]
        private void DoCallback(Guid relatedActivityId)
        {
            WriteEventWithRelatedActivityId(3, relatedActivityId);
        }
    }

    [EventSource(Name = "Microsoft-Orleans-OutsideRuntimeClient")]
    internal sealed class OrleansOutsideRuntimeClientEvent : EventSource
    {
        public static readonly OrleansOutsideRuntimeClientEvent Log = new OrleansOutsideRuntimeClientEvent();
        
        [NonEvent]
        public void SendRequest(Message message)
        {
            if (IsEnabled() && message.TraceContext is TraceContext traceContext)
            {
                SendRequest(traceContext.ActivityId);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(1)]
        private void SendRequest(Guid relatedActivityId)
        {
            WriteEventWithRelatedActivityId(1, relatedActivityId);
        }

        [NonEvent]
        public void ReceiveResponse(Message message)
        {
            if (IsEnabled() && message.TraceContext is TraceContext traceContext)
            {
                ReceiveResponse(traceContext.ActivityId);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(2)]
        private void ReceiveResponse(Guid relatedActivityId)
        {
            WriteEventWithRelatedActivityId(2, relatedActivityId);
        }

        [NonEvent]
        public void SendResponse(Message message)
        {
            if (IsEnabled() && message.TraceContext is TraceContext traceContext)
            {
                SendResponse(traceContext.ActivityId);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(3)]
        private void SendResponse(Guid relatedActivityId)
        {
            WriteEventWithRelatedActivityId(3, relatedActivityId);
        }
    }
}
