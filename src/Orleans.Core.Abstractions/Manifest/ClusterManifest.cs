using System;
using System.Collections;
using System.Collections.Immutable;
using Orleans.Runtime;

namespace Orleans.Metadata
{
    /// <summary>
    /// Information about types which are available in the cluster.
    /// </summary>
    [Serializable]
    [Hagar.GenerateSerializer]
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
        [Hagar.Id(1)]
        public MajorMinorVersion Version { get; }

        /// <summary>
        /// Manifests for each silo in the cluster.
        /// </summary>
        [Hagar.Id(2)]
        public ImmutableDictionary<SiloAddress, GrainManifest> Silos { get; }

        /// <summary>
        /// All grain manifests.
        /// </summary>
        [Hagar.Id(3)]
        public ImmutableArray<GrainManifest> AllGrainManifests { get; }
    }
}
