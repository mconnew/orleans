using System;
using Orleans.Runtime.Scheduler;
using Orleans.Runtime.Versions;
using Orleans.Runtime.Versions.Compatibility;

namespace Orleans.Runtime
{
    internal class ActivationMessagePump
    {
        private Dispatcher dispatcher;
        private readonly Catalog catalog;
        private readonly GrainVersionManifest versionManifest;
        private readonly RuntimeMessagingTrace messagingTrace;
        private readonly ActivationCollector activationCollector;
        private readonly CompatibilityDirectorManager compatibilityDirectorManager;
        private readonly OrleansTaskScheduler scheduler;

        public ActivationMessagePump(
            Catalog catalog,
            GrainVersionManifest versionManifest,
            RuntimeMessagingTrace messagingTrace,
            ActivationCollector activationCollector,
            OrleansTaskScheduler scheduler,
            CompatibilityDirectorManager compatibilityDirectorManager)
        {
            this.catalog = catalog;
            this.versionManifest = versionManifest;
            this.messagingTrace = messagingTrace;
            this.activationCollector = activationCollector;
            this.scheduler = scheduler;
            this.compatibilityDirectorManager = compatibilityDirectorManager;
        }

        internal void SetDispatcher(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        /// <summary>
        /// Receive a new message:
        /// - validate order constraints, queue (or possibly redirect) if out of order
        /// - validate transactions constraints
        /// - invoke handler if ready, otherwise enqueue for later invocation
        /// </summary>
        public void ReceiveMessage(IGrainContext target, Message message)
        {
            var activation = (ActivationData)target;
            this.messagingTrace.OnDispatcherReceiveMessage(message);

            // Don't process messages that have already timed out
            if (message.IsExpired)
            {
                MessagingProcessingStatisticsGroup.OnDispatcherMessageProcessedError(message);
                this.messagingTrace.OnDropExpiredMessage(message, MessagingStatisticsGroup.Phase.Dispatch);
                return;
            }

            if (message.Direction == Message.Directions.Response)
            {
                ReceiveResponse(message, activation);
            }
            else // Request or OneWay
            {
                if (activation.State == ActivationState.Valid)
                {
                    this.activationCollector.TryRescheduleCollection(activation);
                }

                // Silo is always capable to accept a new request. It's up to the activation to handle its internal state.
                // If activation is shutting down, it will queue and later forward this request.
                ReceiveRequest(message, activation);
            }
        }

        /// <summary>
        /// Invoked when an activation has finished a transaction and may be ready for additional transactions
        /// </summary>
        /// <param name="activation">The activation that has just completed processing this message</param>
        /// <param name="message">The message that has just completed processing. 
        /// This will be <c>null</c> for the case of completion of Activate/Deactivate calls.</param>
        internal void OnActivationCompletedRequest(ActivationData activation, Message message)
        {
            lock (activation)
            {
                activation.ResetRunning(message);

                // ensure inactive callbacks get run even with transactions disabled
                if (!activation.IsCurrentlyExecuting)
                    activation.RunOnInactive();

                // Run message pump to see if there is a new request arrived to be processed
                RunMessagePump(activation);
            }
        }

        internal void RunMessagePump(ActivationData activation)
        {
            // Note: this method must be called while holding lock (activation)
            // don't run any messages if activation is not ready or deactivating
            if (activation.State != ActivationState.Valid) return;

            bool runLoop;
            do
            {
                runLoop = false;
                var nextMessage = activation.PeekNextWaitingMessage();
                if (nextMessage == null) continue;
                if (!ActivationMayAcceptRequest(activation, nextMessage)) continue;

                activation.DequeueNextWaitingMessage();
                // we might be over-writing an already running read only request.
                HandleIncomingRequest(nextMessage, activation);
                runLoop = true;
            }
            while (runLoop);
        }

        /// <summary>
        /// Enqueue message for local handling after transaction completes
        /// </summary>
        /// <param name="message"></param>
        /// <param name="targetActivation"></param>
        private void EnqueueRequest(Message message, ActivationData targetActivation)
        {
            var overloadException = targetActivation.CheckOverloaded();
            if (overloadException != null)
            {
                MessagingProcessingStatisticsGroup.OnDispatcherMessageProcessedError(message);
                this.dispatcher.RejectMessage(message, Message.RejectionTypes.Overloaded, overloadException, "Target activation is overloaded " + targetActivation);
                return;
            }

            switch (targetActivation.EnqueueMessage(message))
            {
                case ActivationData.EnqueueMessageResult.Success:
                    // Great, nothing to do
                    break;
                case ActivationData.EnqueueMessageResult.ErrorInvalidActivation:
                    this.dispatcher.ProcessRequestToInvalidActivation(message, targetActivation.Address, targetActivation.ForwardingAddress, "EnqueueRequest");
                    break;
                case ActivationData.EnqueueMessageResult.ErrorActivateFailed:
                    this.dispatcher.ProcessRequestToInvalidActivation(message, targetActivation.Address, targetActivation.ForwardingAddress, "EnqueueRequest", rejectMessages: true);
                    break;
                case ActivationData.EnqueueMessageResult.ErrorStuckActivation:
                    // Avoid any new call to this activation
                    this.dispatcher.ProcessRequestToStuckActivation(message, targetActivation, "EnqueueRequest - blocked grain");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Dont count this as end of processing. The message will come back after queueing via HandleIncomingRequest.
        }

        private void ReceiveResponse(Message message, ActivationData targetActivation)
        {
            lock (targetActivation)
            {
                if (targetActivation.State == ActivationState.Invalid || targetActivation.State == ActivationState.FailedToActivate)
                {
                    this.messagingTrace.OnDispatcherReceiveInvalidActivation(message, targetActivation.State);
                    return;
                }

                MessagingProcessingStatisticsGroup.OnDispatcherMessageProcessedOk(message);

                this.catalog.RuntimeClient.ReceiveResponse(message);
            }
        }

        /// <summary>
        /// Check if we can locally accept this message.
        /// Redirects if it can't be accepted.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="targetActivation"></param>
        private void ReceiveRequest(Message message, ActivationData targetActivation)
        {
            lock (targetActivation)
            {
                if (!ActivationMayAcceptRequest(targetActivation, message))
                {
                    EnqueueRequest(message, targetActivation);
                }
                else
                {
                    HandleIncomingRequest(message, targetActivation);
                }
            }
        }

        /// <summary>
        /// Determine if the activation is able to currently accept the given message
        /// - always accept responses
        /// For other messages, require that:
        /// - activation is properly initialized
        /// - the message would not cause a reentrancy conflict
        /// </summary>
        /// <param name="targetActivation"></param>
        /// <param name="incoming"></param>
        /// <returns></returns>
        private bool ActivationMayAcceptRequest(ActivationData targetActivation, Message incoming)
        {
            if (targetActivation.State != ActivationState.Valid) return false;
            if (!targetActivation.IsCurrentlyExecuting) return true;
            return CanInterleave(targetActivation, incoming);
        }

        /// <summary>
        /// Whether an incoming message can interleave 
        /// </summary>
        /// <param name="targetActivation"></param>
        /// <param name="incoming"></param>
        /// <returns></returns>
        public bool CanInterleave(ActivationData targetActivation, Message incoming)
        {
            if (incoming.IsAlwaysInterleave)
            {
                return true;
            }

            if (targetActivation.Blocking is null)
            {
                return true;
            }

            if (targetActivation.Blocking.IsReadOnly && incoming.IsReadOnly)
            {
                return true;
            }

            if (targetActivation.GetComponent<GrainCanInterleave>() is GrainCanInterleave canInterleave)
            {
                return canInterleave.MayInterleave(incoming);
            }

            return false;
        }

        /// <summary>
        /// Handle an incoming message and queue/invoke appropriate handler
        /// </summary>
        /// <param name="message"></param>
        /// <param name="targetActivation"></param>
        public void HandleIncomingRequest(Message message, ActivationData targetActivation)
        {
            lock (targetActivation)
            {
                if (targetActivation.State == ActivationState.Invalid || targetActivation.State == ActivationState.FailedToActivate)
                {
                    this.dispatcher.ProcessRequestToInvalidActivation(
                        message,
                        targetActivation.Address,
                        targetActivation.ForwardingAddress,
                        "HandleIncomingRequest",
                        rejectMessages: targetActivation.State == ActivationState.FailedToActivate);
                    return;
                }

                if (message.InterfaceVersion > 0)
                {
                    var compatibilityDirector = compatibilityDirectorManager.GetDirector(message.InterfaceType);
                    var currentVersion = versionManifest.GetLocalVersion(message.InterfaceType);
                    if (!compatibilityDirector.IsCompatible(message.InterfaceVersion, currentVersion))
                    {
                        catalog.DeactivateActivationOnIdle(targetActivation);
                        this.dispatcher.ProcessRequestToInvalidActivation(
                            message,
                            targetActivation.Address,
                            targetActivation.ForwardingAddress,
                            "HandleIncomingRequest - Incompatible request");
                        return;
                    }
                }

                // Now we can actually scheduler processing of this request
                targetActivation.RecordRunning(message, message.IsAlwaysInterleave);

                MessagingProcessingStatisticsGroup.OnDispatcherMessageProcessedOk(message);
                this.messagingTrace.OnScheduleMessage(message);
                scheduler.QueueWorkItem(new InvokeWorkItem(targetActivation, message, this.dispatcher.RuntimeClient, this));
            }
        }
    }
}
