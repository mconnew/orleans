using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Orleans.ApplicationParts;
using Orleans.Runtime;

namespace Orleans.Metadata
{
    public class LocalSiloManifestProvider
    {
        public LocalSiloManifestProvider(
            IEnumerable<IGrainMetadataProvider> grainMetadataProviders,
            IEnumerable<IGrainInterfaceMetadataProvider> grainInterfaceMetadataProviders,
            IApplicationPartManager applicationPartManager,
            GrainTypeProvider typeProvider,
            GrainInterfaceIdProvider interfaceIdProvider)
        {
            var grains = CreateGrainMetadata(grainMetadataProviders, applicationPartManager, typeProvider);
            var interfaces = CreateInterfaceMetadata(grainInterfaceMetadataProviders, applicationPartManager, interfaceIdProvider);
            this.SiloManifest = new SiloManifest(grains, interfaces);
        }

        public SiloManifest SiloManifest { get; }

        private static ImmutableDictionary<GrainInterfaceId, cGrainInterfaceMetadata> CreateInterfaceMetadata(
            IEnumerable<IGrainInterfaceMetadataProvider> grainInterfaceMetadataProviders,
            IApplicationPartManager applicationPartManager,
            GrainInterfaceIdProvider grainInterfaceIdProvider)
        {
            var feature = applicationPartManager.CreateAndPopulateFeature<GrainInterfaceFeature>();
            var builder = ImmutableDictionary.CreateBuilder<GrainInterfaceId, cGrainInterfaceMetadata>();
            foreach (var value in feature.Interfaces)
            {
                var interfaceId = grainInterfaceIdProvider.GetGrainInterfaceId(value.InterfaceType);
                var properties = new Dictionary<string, string>();
                foreach (var provider in grainInterfaceMetadataProviders)
                {
                    provider.Populate(value.InterfaceType, interfaceId, properties);
                }

                var metadata = new cGrainInterfaceMetadata(properties.ToImmutableDictionary());
                if (builder.ContainsKey(interfaceId))
                {
                    throw new InvalidOperationException($"An entry with the key {interfaceId} is already present."
                        + $"\nExisting: {builder[interfaceId].ToDetailedString()}\nTrying to add: {metadata.ToDetailedString()}"
                        + "\nConsider using the [GrainInterfaceId(\"name\")] attribute to give these interfaces unique names.");
                }

                builder.Add(interfaceId, metadata);
            }

            return builder.ToImmutable();
        }

        private static ImmutableDictionary<GrainType, GrainMetadata> CreateGrainMetadata(
            IEnumerable<IGrainMetadataProvider> grainMetadataProviders,
            IApplicationPartManager applicationPartManager,
            GrainTypeProvider grainTypeProvider)
        {
            var feature = applicationPartManager.CreateAndPopulateFeature<GrainClassFeature>();
            var builder = ImmutableDictionary.CreateBuilder<GrainType, GrainMetadata>();
            foreach (var value in feature.Classes)
            {
                var grainClass = value.ClassType;
                var grainType = grainTypeProvider.GetGrainType(grainClass);
                var properties = new Dictionary<string, string>();
                foreach (var provider in grainMetadataProviders)
                {
                    provider.Populate(grainClass, grainType, properties);
                }

                var metadata = new GrainMetadata(properties.ToImmutableDictionary());
                if (builder.ContainsKey(grainType))
                {
                    throw new InvalidOperationException($"An entry with the key {grainType} is already present."
                        + $"\nExisting: {builder[grainType].ToDetailedString()}\nTrying to add: {metadata.ToDetailedString()}"
                        + "\nConsider using the [GrainType(\"name\")] attribute to give these classes unique names.");
                }

                builder.Add(grainType, metadata);
            }

            return builder.ToImmutable();
        }
    }
}
