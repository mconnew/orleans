using Microsoft.Extensions.Logging;

namespace Orleans.Runtime.Messaging
{
    internal sealed class MessageTrace
    {
        private readonly ILogger logger;

        public MessageTrace(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger("Orleans.Messaging");
        }

        public void OnHandleMessage(Message message)
        {
            if (this.logger.IsEnabled(LogLevel.Trace)) this.logger.LogTrace("Handling Message {Message}", message);
        }

        internal void OnDropMessage(Message message, string reason)
        {
            if (this.logger.IsEnabled(LogLevel.Trace)) this.logger.LogTrace("Dropping Message {Message}. Reason: {Reason}", message, reason);
        }

        internal void OnRejectMessage(Message message, string reason)
        {
            if (this.logger.IsEnabled(LogLevel.Trace)) this.logger.LogTrace("Rejecting Message {Message}. Reason: {Reason}", message, reason);
        }

        internal void OnInboundPing(Message message)
        {
            if (this.logger.IsEnabled(LogLevel.Trace)) this.logger.LogTrace("Received ping message {Message}", message);
        }
    }
}
