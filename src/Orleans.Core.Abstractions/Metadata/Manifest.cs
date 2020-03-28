using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Orleans.Runtime;

namespace Orleans.Metadata
{
    /// <summary>
    /// Information about available a silo.
    /// </summary>
    [Serializable]
    public class SiloManifest
    {
        // Map from formatted CLR type name -> interface metadata
        public ImmutableDictionary<GrainInterfaceId, cGrainInterfaceMetadata> Interfaces { get; }

        // Map from grain type -> grain (class) metadata
        // Should this be called 'routes'?
        // Can GrainType ever be a template? Eg, how are generics encoded into GrainType?
        public ImmutableDictionary<GrainType, GrainMetadata> Grains { get; }
    }

    /// <summary>
    /// Information about types which are available in the cluster.
    /// </summary>
    [Serializable]
    public class ClusterManifest
    {
        /// <summary>
        /// Manifests for each silo in the cluster.
        /// </summary>
        public ImmutableDictionary<SiloAddress, SiloManifest> Silos { get; }
    }
}
