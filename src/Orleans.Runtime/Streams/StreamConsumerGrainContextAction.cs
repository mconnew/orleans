using System;
using Orleans.Runtime;
using Orleans.Streams.Core;

namespace Orleans.Streams
{
    internal class StreamConsumerGrainContextAction : IConfigureGrain
    {
        private readonly GrainTypeManager grainTypeManager;
        private readonly IStreamProviderRuntime streamProviderRuntime;

        public StreamConsumerGrainContextAction(
            GrainTypeManager grainTypeManager,
            IStreamProviderRuntime streamProviderRuntime)
        {
            this.grainTypeManager = grainTypeManager;
            this.streamProviderRuntime = streamProviderRuntime;
        }

        public bool CanConfigure(GrainType grainType) => true;

        public void Configure(IGrainContext context)
        {
            if (context.GrainInstance is IStreamSubscriptionObserver observer && context is ActivationData data)
            {
                InstallStreamConsumerExtension(data, observer as IStreamSubscriptionObserver);
            }
        }

        private void InstallStreamConsumerExtension(ActivationData result, IStreamSubscriptionObserver observer)
        {
            var invoker = InsideRuntimeClient.TryGetExtensionMethodInvoker(this.grainTypeManager, typeof(IStreamConsumerExtension));
            if (invoker == null)
            {
                throw new InvalidOperationException("Extension method invoker was not generated for an extension interface");
            }

            var handler = new StreamConsumerExtension(this.streamProviderRuntime, observer);
            result.ExtensionInvoker.TryAddExtension(invoker, handler);
        }
    }
}
