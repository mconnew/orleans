
using System;
using System.Collections.Generic;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;

namespace Orleans.Providers.Streams.Generator
{
    [Orleans.GenerateSerializer]
    internal class GeneratedBatchContainer : IBatchContainer
    {
        [Orleans.Id(0)]
        public StreamId StreamId { get; }

        public StreamSequenceToken SequenceToken => RealToken;

        [Orleans.Id(1)]
        public EventSequenceTokenV2 RealToken { get;  }

        [Orleans.Id(2)]
        public DateTime EnqueueTimeUtc { get; }

        [Orleans.Id(3)]
        public object Payload { get; }

        public GeneratedBatchContainer(StreamId streamId, object payload, EventSequenceTokenV2 token)
        {
            StreamId = streamId;
            EnqueueTimeUtc = DateTime.UtcNow;
            this.Payload = payload;
            this.RealToken = token;
        }

        public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
        {
            return new[] { Tuple.Create((T)Payload, SequenceToken) };
        }

        public bool ImportRequestContext()
        {
            return false;
        }
    }
}
