using System;
using Orleans.Runtime;
using Orleans.Streams.Core;

namespace Orleans.Streams
{
    internal class StreamConsumerGrainContextAction : IConfigureGrain
    {
        private readonly IStreamProviderRuntime _streamProviderRuntime;

        public StreamConsumerGrainContextAction(
            IStreamProviderRuntime streamProviderRuntime)
        {
            _streamProviderRuntime = streamProviderRuntime;
        }

        public bool CanConfigure(GrainType grainType) => true;

        public void Configure(IGrainContext context)
        {
            if (context.GrainInstance is IStreamSubscriptionObserver observer)
            {
                InstallStreamConsumerExtension(context, observer as IStreamSubscriptionObserver);
            }
        }

        private void InstallStreamConsumerExtension(IGrainContext context, IStreamSubscriptionObserver observer)
        {
            var handler = new StreamConsumerExtension(this._streamProviderRuntime, observer);
            context.SetComponent<IStreamConsumerExtension>(handler);
        }
    }
}
