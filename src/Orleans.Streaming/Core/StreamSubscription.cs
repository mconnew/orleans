using System;
using Orleans.Runtime;

namespace Orleans.Streams.Core
{
    [Serializable]
    [Immutable]
    [Orleans.GenerateSerializer]
    public class StreamSubscription
    {
        public StreamSubscription(Guid subscriptionId, string streamProviderName, StreamId streamId, GrainId grainId)
        {
            this.SubscriptionId = subscriptionId;
            this.StreamProviderName = streamProviderName;
            this.StreamId = streamId;
            this.GrainId = grainId;
        }

        [Orleans.Id(1)]
        public Guid SubscriptionId { get; set; }
        [Orleans.Id(2)]
        public string StreamProviderName { get; set; }
        [Orleans.Id(3)]
        public StreamId StreamId { get; set; }
        [Orleans.Id(4)]
        public GrainId GrainId { get; set; }
    }
}
