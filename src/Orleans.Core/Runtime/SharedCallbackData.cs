using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Serialization;

namespace Orleans.Runtime
{
    internal class SharedCallbackData
    {
        public readonly Action<Message> Unregister;
        public readonly ILogger Logger;
        public readonly MessagingOptions MessagingOptions;
        private TimeSpan responseTimeout;
        public long ResponseTimeoutStopwatchTicks;

        public SharedCallbackData(
            Action<Message> unregister,
            ILogger logger,
            MessagingOptions messagingOptions,
            ApplicationRequestsStatisticsGroup requestStatistics,
            TimeSpan responseTimeout)
        {
            RequestStatistics = requestStatistics;
            this.Unregister = unregister;
            this.Logger = logger;
            this.MessagingOptions = messagingOptions;
            this.ResponseTimeout = responseTimeout;
        }

        public ApplicationRequestsStatisticsGroup RequestStatistics { get; }

        public TimeSpan ResponseTimeout
        {
            get => this.responseTimeout;
            set
            {
                this.responseTimeout = value;
                this.ResponseTimeoutStopwatchTicks = (long)(value.TotalSeconds * Stopwatch.Frequency);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public void ResponseCallback(Message message, TaskCompletionSource<object> context)
        {
            if (message.Result == Message.ResponseTypes.Rejection)
            {
                switch (message.RejectionType)
                {
                    case Message.RejectionTypes.GatewayTooBusy:
                        context.TrySetException(new GatewayTooBusyException());
                        break;

                    default:
                        var rejection = message.GetBodyObject<Exception>();
                        if (rejection is Exception exception)
                        {
                            context.TrySetException(exception);
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(message.RejectionInfo))
                            {
                                message.RejectionInfo = "Unable to send request - no rejection info available";
                            }

                            context.TrySetException(new OrleansMessageRejectionException(message.RejectionInfo));
                        }

                        break;
                }

                return;
            }

            try
            {
                var response = message.GetBodyObject<Response>();

                if (!response.ExceptionFlag)
                {
                    context.TrySetResult(response.Data);
                }
                else
                {
                    context.TrySetException((Exception)response.Data);
                }
            }
            catch (Exception exc)
            {
                // Catch the deserialization exception and break the promise with it.
                context.TrySetException(exc);
            }
        }
    }
}