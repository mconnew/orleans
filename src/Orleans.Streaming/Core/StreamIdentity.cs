
using System;

namespace Orleans.Streams
{
    /// <summary>
    /// Stream identity contains the public stream information use to uniquely identify a stream.
    /// Stream identities are only unique per stream provider.
    /// </summary>
    [Serializable]
    [GenerateSerializer]
    public class StreamIdentity : IStreamIdentity
    {
        public StreamIdentity(Guid streamGuid, string streamNamespace)
        {
            Guid = streamGuid;
            Namespace = streamNamespace;
        }

        /// <summary>
        /// Stream primary key guid.
        /// </summary>
        [Id(1)]
        public Guid Guid { get; }

        /// <summary>
        /// Stream namespace.
        /// </summary>
        [Id(2)]
        public string Namespace { get; }
    }
}
