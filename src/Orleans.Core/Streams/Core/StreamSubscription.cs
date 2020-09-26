using Orleans.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Orleans.Streams.Core
{
    [Serializable]
    [Immutable]
    [Hagar.GenerateSerializer]
    public class StreamSubscription
    {
        public StreamSubscription(Guid subscriptionId, string streamProviderName, StreamId streamId, GrainId grainId)
        {
            this.SubscriptionId = subscriptionId;
            this.StreamProviderName = streamProviderName;
            this.StreamId = streamId;
            this.GrainId = grainId;
        }

        [Hagar.Id(1)]
        public Guid SubscriptionId { get; set; }
        [Hagar.Id(2)]
        public string StreamProviderName { get; set; }
        [Hagar.Id(3)]
        public StreamId StreamId { get; set; }
        [Hagar.Id(4)]
        public GrainId GrainId { get; set; }
    }
}
