using System;
using System.Collections.Immutable;
using System.Linq;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Orleans.MetadataStore
{
    [Immutable]
    [Serializable]
    [GenerateSerializer]
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
            Stamp = stamp;
            Version = version;
            Nodes = nodes;
            AcceptQuorum = acceptQuorum;
            PrepareQuorum = prepareQuorum;
            Ranges = ranges;
            Values = values ?? ImmutableDictionary<string, string>.Empty;
        }

        /// <summary>
        /// The addresses of all nodes.
        /// </summary>
        [Id(0)]
        public SiloAddress[] Nodes { get; }

        /// <summary>
        /// The quorum size for Accept operations.
        /// </summary>
        [Id(1)]
        public int AcceptQuorum { get; }

        /// <summary>
        /// The quorum size for Prepare operations.
        /// </summary>
        [Id(2)]
        public int PrepareQuorum { get; }

        /// <summary>
        /// The unique ballot number of this configuration.
        /// </summary>
        [Id(3)]
        public Ballot Stamp { get; }

        /// <summary>
        /// The monotonically increasing version number of this configuration.
        /// </summary>
        [Id(4)]
        public long Version { get; }

        /// <summary>
        /// The partition range map, which divides a keyspace into a set of arbitrarily-sized partitions.
        /// </summary>
        [Id(5)]
        public RangeMap Ranges { get; }

        /// <summary>
        /// Additional data stored with this configuration.
        /// </summary>
        [Id(6)]
        public ImmutableDictionary<string, string> Values { get; } = ImmutableDictionary<string, string>.Empty;

        /// <inheritdoc />
        public override string ToString()
        {
            var nodes = Nodes == null ? "[]" : $"[{string.Join(", ", Nodes.Select(_ => _.ToString()))}]";
            var values = (Values is null || Values.Count == 0) ? "[]" : $"[{string.Join(", ", Values.Select(_ => $"\"{_.Key}\"=\"{_.Value}\""))}]";
            return $"{nameof(Stamp)}: {Stamp}, {nameof(Version)}: {Version}, {nameof(Nodes)}: {nodes}, {nameof(AcceptQuorum)}: {AcceptQuorum}, {nameof(PrepareQuorum)}: {PrepareQuorum}, {nameof(Values)}: {values}";
        }
    }
}
