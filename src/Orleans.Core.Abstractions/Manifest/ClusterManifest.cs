using System;
using System.Collections;
using System.Collections.Immutable;
using Hagar;
using Orleans.Runtime;

namespace Orleans.Metadata
{
    /// <summary>
    /// Information about types which are available in the cluster.
    /// </summary>
    [Serializable]
    [Orleans.GenerateSerializer]
    [Orleans.Concurrency.Immutable]
    [Immutable]
    public class ClusterManifest
    {
        /// <summary>
        /// Creates a new <see cref="ClusterManifest"/> instance.
        /// </summary>
        public ClusterManifest(
            MajorMinorVersion version,
            ImmutableDictionary<SiloAddress, GrainManifest> silos,
            ImmutableArray<GrainManifest> allGrainManifests)
        {
            this.Version = version;
            this.Silos = silos;
            this.AllGrainManifests = allGrainManifests;
        }

        /// <summary>
        /// The version of this instance.
        /// </summary>
        [Orleans.Id(1)]
        public MajorMinorVersion Version { get; }

        /// <summary>
        /// Manifests for each silo in the cluster.
        /// </summary>
        [Orleans.Id(2)]
        public ImmutableDictionary<SiloAddress, GrainManifest> Silos { get; }

        /// <summary>
        /// All grain manifests.
        /// </summary>
        [Orleans.Id(3)]
        public ImmutableArray<GrainManifest> AllGrainManifests { get; }
    }
}
