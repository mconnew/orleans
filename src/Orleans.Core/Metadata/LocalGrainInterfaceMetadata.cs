using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Orleans.ApplicationParts;
using Orleans.Runtime;
using Orleans.Utilities;

namespace Orleans.Metadata
{
    /// <summary>
    /// Provides access to metadata for locally available grain types.
    /// </summary>
    public class LocalGrainInterfaceMetadata
    {
        public LocalGrainInterfaceMetadata(
            IApplicationPartManager applicationPartManager,
            IEnumerable<IGrainInterfaceMetadataProvider> grainInterfaceMetadataProviders)
        {
            this.GrainInterfaceMetadata = BuildGrainInterfaceMetadata(grainInterfaceMetadataProviders, applicationPartManager);
        }

        private static ImmutableDictionary<GrainInterfaceId, GrainInterfaceMetadata> BuildGrainInterfaceMetadata(
            IEnumerable<IGrainInterfaceMetadataProvider> providers,
            IApplicationPartManager applicationPartManager)
        {
            var feature = applicationPartManager.CreateAndPopulateFeature<GrainInterfaceFeature>();
            var builder = ImmutableDictionary.CreateBuilder<GrainInterfaceId, GrainInterfaceMetadata>();
            foreach (var grainClassMetadata in feature.Interfaces)
            {
                var interfaceType = grainClassMetadata.InterfaceType;
                var interfaceId = new GrainInterfaceId(RuntimeTypeNameFormatter.Format(interfaceType));
                var properties = new Dictionary<string, string>();
                foreach (var provider in providers)
                {
                    provider.Populate(interfaceType, properties);
                }

                var grainMetadata = new GrainInterfaceMetadata(properties.ToImmutableDictionary());
                if (builder.ContainsKey(interfaceId))
                {
                    throw new InvalidOperationException($"An entry with the key {interfaceId} is already present."
                        + $"\nExisting: {builder[interfaceId].ToDetailedString()}\nTrying to add: {grainMetadata.ToDetailedString()}");
                }

                builder.Add(interfaceId, grainMetadata);
            }

            return builder.ToImmutableDictionary();
        }

        public ImmutableDictionary<GrainInterfaceId, GrainInterfaceMetadata> GrainInterfaceMetadata { get; }
    }

    public class GrainTypeInterfaceDescriptor
    {
        public GrainType GrainType { get; set; }

        // NOTE! This is NOT Unordered - it is the opposite.
        public bool Ordered { get; set; }

        public int Version { get; set; }
    }

    public class ClusterGrainInterfaceMap
    {

    }

    public interface IClusterGrainMetadataProvider
    {
        ValueTask<ClusterGrainInterfaceMap> GetGrainInterfaceMap();
    }

    /// <summary>
    /// Provides access to metadata for grain types which are available in the cluster.
    /// </summary>
    public class ClusterGrainMetadata
    {
        public ClusterGrainMetadata(
            IApplicationPartManager applicationPartManager,
            IEnumerable<IGrainInterfaceMetadataProvider> grainInterfaceMetadataProviders)
        {
            this.GrainInterfaceMetadata = BuildGrainInterfaceMetadata(grainInterfaceMetadataProviders, applicationPartManager);
        }

        private static ImmutableDictionary<GrainInterfaceId, GrainInterfaceMetadata> BuildGrainInterfaceMetadata(
            IEnumerable<IGrainInterfaceMetadataProvider> providers,
            IApplicationPartManager applicationPartManager)
        {
            var feature = applicationPartManager.CreateAndPopulateFeature<GrainInterfaceFeature>();
            var builder = ImmutableDictionary.CreateBuilder<GrainInterfaceId, GrainInterfaceMetadata>();
            foreach (var grainClassMetadata in feature.Interfaces)
            {
                var interfaceType = grainClassMetadata.InterfaceType;
                var interfaceId = new GrainInterfaceId(RuntimeTypeNameFormatter.Format(interfaceType));
                var properties = new Dictionary<string, string>();
                foreach (var provider in providers)
                {
                    provider.Populate(interfaceType, properties);
                }

                var grainMetadata = new GrainInterfaceMetadata(properties.ToImmutableDictionary());
                if (builder.ContainsKey(interfaceId))
                {
                    throw new InvalidOperationException($"An entry with the key {interfaceId} is already present."
                        + $"\nExisting: {builder[interfaceId].ToDetailedString()}\nTrying to add: {grainMetadata.ToDetailedString()}");
                }

                builder.Add(interfaceId, grainMetadata);
            }

            return builder.ToImmutableDictionary();
        }

        public ImmutableDictionary<GrainInterfaceId, GrainInterfaceMetadata> GrainInterfaceMetadata { get; }
    }
}
