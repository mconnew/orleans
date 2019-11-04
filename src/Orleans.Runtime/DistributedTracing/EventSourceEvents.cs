using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Text;

namespace Orleans.Runtime
{
    [EventSource(Name = "Microsoft-Orleans-Dispatcher")]
    internal sealed class OrleansDispatcherEvent : EventSource
    {
        public static readonly OrleansDispatcherEvent Log = new OrleansDispatcherEvent();

        [NonEvent]
        public void ReceiveMessage(Message message)
        {
            if (IsEnabled() && message.TraceContext is TraceContext traceContext)
            {
                ReceiveMessage(traceContext.ActivityId);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(1)]
        private void ReceiveMessage(Guid relatedActivityId)
        {
            WriteEventWithRelatedActivityId(1, relatedActivityId);
        }
    }

    [EventSource(Name = "Microsoft-Orleans-InsideRuntimeClient")]
    internal sealed class OrleansInsideRuntimeClientEvent : EventSource
    {
        public static readonly OrleansInsideRuntimeClientEvent Log = new OrleansInsideRuntimeClientEvent();

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

    [EventSource(Name = "Microsoft-Orleans-GatewayAcceptor")]
    internal sealed class OrleansGatewayAcceptorEvent : EventSource
    {
        public static readonly OrleansGatewayAcceptorEvent Log = new OrleansGatewayAcceptorEvent();

        [NonEvent]
        public void HandleMessage(Message message)
        {
            if (IsEnabled() && message.TraceContext is TraceContext traceContext)
            {
                HandleMessage(traceContext.ActivityId);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(1)]
        private void HandleMessage(Guid relatedActivityId)
        {
            WriteEventWithRelatedActivityId(1, relatedActivityId);
        }
    }

    [EventSource(Name = "Microsoft-Orleans-IncomingMessageAcceptor")]
    internal sealed class OrleansIncomingMessageAcceptorEvent : EventSource
    {
        public static readonly OrleansIncomingMessageAcceptorEvent Log = new OrleansIncomingMessageAcceptorEvent();

        [NonEvent]
        public void HandleMessage(Message message)
        {
            if (IsEnabled() && message.TraceContext is TraceContext traceContext)
            {
                HandleMessage(traceContext.ActivityId);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(1)]
        private void HandleMessage(Guid relatedActivityId)
        {
            WriteEventWithRelatedActivityId(1, relatedActivityId);
        }
    }

    [EventSource(Name = "Microsoft-Orleans-IncomingMessageAgent")]
    internal sealed class OrleansIncomingMessageAgentEvent : EventSource
    {
        public static readonly OrleansIncomingMessageAgentEvent Log = new OrleansIncomingMessageAgentEvent();

        [NonEvent]
        public void ReceiveMessage(Message message)
        {
            if (IsEnabled() && message.TraceContext is TraceContext traceContext)
            {
                ReceiveMessage(traceContext.ActivityId);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Event(1)]
        private void ReceiveMessage(Guid relatedActivityId)
        {
            WriteEventWithRelatedActivityId(1, relatedActivityId);
        }
    }
}
