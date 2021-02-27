using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Orleans.Providers.GCP.Streams.PubSub
{
    /// <summary>
    /// Converts event data to and from cloud queue message
    /// </summary>
    public interface IPubSubDataAdapter
    {
        /// <summary>
        /// Creates a <seealso cref="PubsubMessage"/> from stream event data.
        /// </summary>
        PubsubMessage ToPubSubMessage<T>(StreamId streamId, IEnumerable<T> events, Dictionary<string, object> requestContext);

        /// <summary>
        /// Creates a batch container from a <seealso cref="PubsubMessage"/> message
        /// </summary>
        IBatchContainer FromPullResponseMessage(PubsubMessage msg, long sequenceId);
    }

    [Hagar.SerializationCallbacks(typeof(OnDeserializedCallbacks))]
    public class PubSubDataAdapter : IPubSubDataAdapter, IOnDeserialized
    {
        private Hagar.Serializer<PubSubBatchContainer> _serializer;

        /// <summary>
        /// Initializes a new instance of the <seealso cref="PubSubDataAdapter"/> class.
        /// </summary>
        /// <param name="serializer">The <seealso cref="SerializationManager"/> injected at runtime.</param>
        public PubSubDataAdapter(Hagar.Serializer<PubSubBatchContainer> serializer)
        {
            _serializer = serializer;
        }

        /// <inherithdoc/>
        public IBatchContainer FromPullResponseMessage(PubsubMessage msg, long sequenceId)
        {
            var batchContainer = _serializer.Deserialize(msg.Data.ToByteArray());
            batchContainer.RealSequenceToken = new EventSequenceTokenV2(sequenceId);
            return batchContainer;
        }

        /// <inherithdoc/>
        public PubsubMessage ToPubSubMessage<T>(StreamId streamId, IEnumerable<T> events, Dictionary<string, object> requestContext)
        {
            var batchMessage = new PubSubBatchContainer(streamId, events.Cast<object>().ToList(), requestContext);
            var rawBytes = _serializer.SerializeToArray(batchMessage);

            return new PubsubMessage { Data = ByteString.CopyFrom(rawBytes) };
        }

        void IOnDeserialized.OnDeserialized(ISerializerContext context)
        {
            _serializer = context.ServiceProvider.GetRequiredService<Hagar.Serializer<PubSubBatchContainer>>();
        }
    }
}
