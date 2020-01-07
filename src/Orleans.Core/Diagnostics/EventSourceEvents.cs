using System;
using System.Diagnostics.Tracing;

namespace Orleans.Runtime
{
    [EventSource(Name = "Microsoft-Orleans-CallBackData")]
    internal sealed class OrleansCallBackDataEvent : EventSource
    {
        public static readonly OrleansCallBackDataEvent Log = new OrleansCallBackDataEvent();

        [NonEvent]
        public void OnTimeout(Message message)
        {
            OnTimeout(message.TraceContext?.ActivityId ?? Guid.Empty);
        }

        [Event(1)]
        public void OnTimeout(Guid activityId)
        {
            this.WriteEventWithRelatedActivityId(1, activityId);
        }

        [NonEvent]
        public void OnTargetSiloFail(Message message)
        {
            OnTargetSiloFail(message.TraceContext?.ActivityId ?? Guid.Empty);
        }

        [Event(2)]
        public void OnTargetSiloFail(Guid activityId)
        {
            this.WriteEventWithRelatedActivityId(2, activityId);
        }

        [NonEvent]
        public void DoCallback(Message message)
        {
            DoCallback(message.TraceContext?.ActivityId ?? Guid.Empty);
        }

        [Event(3)]
        public void DoCallback(Guid activityId)
        {
            WriteEventWithRelatedActivityId(3, activityId);
        }
    }

    [EventSource(Name = "Microsoft-Orleans-OutsideRuntimeClient")]
    internal sealed class OrleansOutsideRuntimeClientEvent : EventSource
    {
        public static readonly OrleansOutsideRuntimeClientEvent Log = new OrleansOutsideRuntimeClientEvent();

        [NonEvent]
        public void SendRequest(Message message)
        {
            SendRequest(message.TraceContext?.ActivityId ?? Guid.Empty);
        }

        [Event(1)]
        public void SendRequest(Guid activityId)
        {
            WriteEventWithRelatedActivityId(1, activityId);
        }

        [NonEvent]
        public void ReceiveResponse(Message message)
        {
            ReceiveResponse(message.TraceContext?.ActivityId ?? Guid.Empty);
        }

        [Event(2)]
        public void ReceiveResponse(Guid activityId)
        {
            WriteEventWithRelatedActivityId(2, activityId);
        }

        [NonEvent]
        public void SendResponse(Message message)
        {
            SendResponse(message.TraceContext?.ActivityId ?? Guid.Empty);
        }

        [Event(3)]
        public void SendResponse(Guid activityId)
        {
            WriteEventWithRelatedActivityId(3, activityId);
        }
    }
}
