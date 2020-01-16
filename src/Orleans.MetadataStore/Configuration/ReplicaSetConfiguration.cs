using System;
using System.Collections.Immutable;
using System.Linq;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Orleans.MetadataStore
{
    [Immutable]
    [Serializable]
    public class ReplicaSetConfiguration : IVersioned
    {
        public ReplicaSetConfiguration(
            Ballot stamp,
            long version,
            SiloAddress[] nodes,
            int acceptQuorum,
            int prepareQuorum,
            RangeMap ranges,
            ImmutableDictionary<string, string> values)
        {
            this.Stamp = stamp;
            this.Version = version;
            this.Nodes = nodes;
            this.AcceptQuorum = acceptQuorum;
            this.PrepareQuorum = prepareQuorum;
            this.Ranges = ranges;
            this.Values = values ?? ImmutableDictionary<string, string>.Empty;
        }

        /// <summary>
        /// The addresses of all nodes.
        /// </summary>
        public SiloAddress[] Nodes { get; }

        /// <summary>
        /// The quorum size for Accept operations.
        /// </summary>
        public int AcceptQuorum { get; }

        /// <summary>
        /// The quorum size for Prepare operations.
        /// </summary>
        public int PrepareQuorum { get; }

        /// <summary>
        /// The unique ballot number of this configuration.
        /// </summary>
        public Ballot Stamp { get; }

        /// <summary>
        /// The monotonically increasing version number of this configuration.
        /// </summary>
        public long Version { get; }

        /// <summary>
        /// The partition range map, which divides a keyspace into a set of arbitrarily-sized partitions.
        /// </summary>
        public RangeMap Ranges { get; }

        /// <summary>
        /// Additional data stored with this configuration.
        /// </summary>
        public ImmutableDictionary<string, string> Values { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            var nodes = this.Nodes == null ? "[]" : $"[{string.Join(", ", this.Nodes.Select(_ => _.ToString()))}]";
            var values = this.Values.Count == 0 ? "[]" : $"[{string.Join(", ", this.Values.Select(_ => $"\"{_.Key}\"=\"{_.Value}\""))}]";
            return $"{nameof(Stamp)}: {Stamp}, {nameof(Version)}: {Version}, {nameof(Nodes)}: {nodes}, {nameof(AcceptQuorum)}: {AcceptQuorum}, {nameof(PrepareQuorum)}: {PrepareQuorum}, {nameof(Values)}: {values}";
        }
    }
}
