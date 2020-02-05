using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Orleans.ApplicationParts;
using Orleans.CodeGeneration;
using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.Utilities;

namespace Orleans.Metadata
{
    /// <summary>
    /// Provides access to metadata for locally available grain types.
    /// </summary>
    public class LocalGrainMetadata
    {
        public LocalGrainMetadata(
            IEnumerable<IGrainMetadataProvider> grainMetadataProviders,
            IApplicationPartManager applicationPartManager,
            GrainTypeProvider grainTypeProvider,
            IEnumerable<IGrainInterfaceMetadataProvider> grainInterfaceMetadataProviders)
        {
            this.GrainMetadata = BuildGrainMetadata(grainMetadataProviders, applicationPartManager, grainTypeProvider);
            this.GrainInterfaceMetadata = BuildGrainInterfaceMetadata(grainInterfaceMetadataProviders, applicationPartManager);
        }

        private static ImmutableDictionary<GrainType, GrainMetadata> BuildGrainMetadata(
            IEnumerable<IGrainMetadataProvider> providers,
            IApplicationPartManager applicationPartManager,
            GrainTypeProvider grainTypeProvider)
        {
            var grainClassFeature = applicationPartManager.CreateAndPopulateFeature<GrainClassFeature>();
            var builder = ImmutableDictionary.CreateBuilder<GrainType, GrainMetadata>();
            foreach (var grainClassMetadata in grainClassFeature.Classes)
            {
                var grainClass = grainClassMetadata.ClassType;
                var grainType = grainTypeProvider.GetGrainType(grainClass);
                var properties = new Dictionary<string, string>();
                foreach (var provider in providers)
                {
                    provider.Populate(grainType, grainClass, properties);
                }

                var grainMetadata = new GrainMetadata(properties.ToImmutableDictionary());
                if (builder.ContainsKey(grainType))
                {
                    throw new InvalidOperationException($"An entry with the key {grainType} is already present."
                        + $"\nExisting: {builder[grainType].ToDetailedString()}\nTrying to add: {grainMetadata.ToDetailedString()}");
                }

                builder.Add(grainType, grainMetadata);
            }

            return builder.ToImmutableDictionary();
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

        public ImmutableDictionary<GrainType, GrainMetadata> GrainMetadata { get; }

        public ImmutableDictionary<GrainInterfaceId, GrainInterfaceMetadata> GrainInterfaceMetadata { get; }
    }

    public static class DefaultGrainMetadataProviders
    {
        public static void Configure(IServiceCollection services)
        {
            services.AddSingleton<IGrainMetadataProvider, AttributeGrainMetadataProvider>();
            services.AddSingleton<IGrainMetadataProvider, LegacyGrainMetadataProvider>();
            services.AddSingleton<IGrainMetadataProvider, GenericGrainMetadataProvider>();
            services.AddSingleton<IGrainMetadataProvider, UnorderedAttributeGrainMetadataProvider>();

            services.AddSingleton<IGrainInterfaceMetadataProvider, AttributeGrainInterfaceMetadataProvider>();

            services.AddSingleton<IGrainTypeProvider, AttributeGrainTypeProvider>();
            services.AddSingleton<IGrainTypeProvider, LegacyGrainTypeProvider>();
        }
    }

    public class AttributeGrainMetadataProvider : IGrainMetadataProvider
    {
        private readonly IServiceProvider serviceProvider;

        public AttributeGrainMetadataProvider(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public void Populate(GrainType grainType, Type grainClass, Dictionary<string, string> properties)
        {
            foreach (var attr in grainClass.GetCustomAttributes(inherit: true))
            {
                if (attr is IGrainMetadataProviderAttribute provider)
                {
                    provider.Populate(this.serviceProvider, grainType, grainClass, properties);
                }
            }
        }
    }

    public class AttributeGrainInterfaceMetadataProvider : IGrainInterfaceMetadataProvider
    {
        private readonly IServiceProvider serviceProvider;

        public AttributeGrainInterfaceMetadataProvider(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public void Populate(Type grainInterface, Dictionary<string, string> properties)
        {
            foreach (var attr in grainInterface.GetCustomAttributes(inherit: true))
            {
                if (attr is IGrainInterfaceMetadataProviderAttribute provider)
                {
                    provider.Populate(this.serviceProvider, grainInterface, properties);
                }
            }
        }
    }

    /// <summary>
    /// Populates the <see cref="WellKnownGrainTypeProperties.Unordered"/> property for grains which have the
    /// <see cref="StatelessWorkerAttribute"/> on the class declaration or <see cref="UnorderedAttribute"/> attribute
    /// on an implemented interface.
    /// </summary>
    public class UnorderedAttributeGrainMetadataProvider : IGrainMetadataProvider
    {
        public void Populate(GrainType grainType, Type grainClass, Dictionary<string, string> properties)
        {
            if (IsUnordered(grainClass))
            {
                AddUnordered(properties);
                return;
            }

            foreach (var interfaceType in grainClass.GetInterfaces())
            {
                if (IsUnordered(interfaceType))
                {
                    AddUnordered(properties);
                    return;
                }
            }

            static bool IsUnordered(Type type)
            {
                foreach (var attr in type.GetCustomAttributes(inherit: true))
                {
                    if (attr is StatelessWorkerAttribute || attr is UnorderedAttribute)
                    {
                        return true;
                    }
                }

                return false;
            }

            static void AddUnordered(Dictionary<string, string> properties)
            {
                properties[WellKnownGrainTypeProperties.Unordered] = "true";
            }
        }
    }

    public class AttributeGrainTypeProvider : IGrainTypeProvider
    {
        private readonly IServiceProvider serviceProvider;

        public AttributeGrainTypeProvider(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public GrainType? GetGrainType(Type grainClass)
        {
            foreach (var attr in grainClass.GetCustomAttributes(inherit: true))
            {
                if (attr is IGrainTypeProviderAttribute typeProviderAttribute)
                {
                    return typeProviderAttribute.GetGrainType(this.serviceProvider, grainClass);
                }
            }

            return default;
        }
    }

    public class LegacyGrainTypeProvider : IGrainTypeProvider
    {
        public GrainType? GetGrainType(Type grainClass)
        {
            if (LegacyGrainId.IsLegacyGrainType(grainClass))
            {
                var typeCode = GrainInterfaceUtils.GetGrainClassTypeCode(grainClass);
                return LegacyGrainId.CreateGrainTypeForGrain(typeCode);
            }

            return default;
        }
    }

    public class LegacyGrainMetadataProvider : IGrainMetadataProvider
    {
        public void Populate(GrainType grainType, Type classType, Dictionary<string, string> properties)
        {
            if (LegacyGrainId.IsLegacyGrainType(classType))
            {
                var baseTypeCode = GrainInterfaceUtils.GetGrainClassTypeCode(classType);
                properties[WellKnownGrainTypeProperties.LegacyGrainIdBaseTypeCode] = baseTypeCode.ToString(CultureInfo.InvariantCulture);
                properties[WellKnownGrainTypeProperties.LegacyGrainIdType] = GetLegacyGrainIdType(classType);
            }
        }

        private static string GetLegacyGrainIdType(Type type)
        {
            if (typeof(IGrainWithGuidKey).IsAssignableFrom(type)) return WellKnownGrainTypeProperties.LegacyGrainIdTypes.Guid;
            else if (typeof(IGrainWithIntegerKey).IsAssignableFrom(type)) return WellKnownGrainTypeProperties.LegacyGrainIdTypes.Integer;
            else if (typeof(IGrainWithStringKey).IsAssignableFrom(type)) return WellKnownGrainTypeProperties.LegacyGrainIdTypes.String;
            else if (typeof(IGrainWithGuidCompoundKey).IsAssignableFrom(type)) return WellKnownGrainTypeProperties.LegacyGrainIdTypes.GuidPlusString;
            else if (typeof(IGrainWithIntegerCompoundKey).IsAssignableFrom(type)) return WellKnownGrainTypeProperties.LegacyGrainIdTypes.IntegerPlusString;

            throw new ArgumentException($"Type {type} is not a legacy grain type");
        }
    }

    /// <summary>
    /// Populates <see cref="WellKnownGrainTypeProperties.GenericParameterCount"/> for any generic grain type.
    /// </summary>
    public class GenericGrainMetadataProvider : IGrainMetadataProvider
    {
        public void Populate(GrainType grainType, Type classType, Dictionary<string, string> properties)
        {
            if (classType.IsGenericType)
            {
                var genericParameterCount = classType.GetGenericArguments().Length;
                properties[WellKnownGrainTypeProperties.GenericParameterCount] = genericParameterCount.ToString(CultureInfo.InvariantCulture);
            }
        }
    }

    // For grain implementations (classes)

    // For metadata associated with 'GrainType' for local or remote grains.
    // Info such as placement policy.
    public static class GrainTypePropertiesExtensions
    {
        // for example...
        public static string GetMultiClusterRegistrationStrategy(this GrainMetadata properties) => properties.Values["sys.multicluster.registration.policy"] ?? "default";
        public static string GetPlacementPolicy(this GrainMetadata properties) => properties.Values["sys.placement.policy"] ?? "random";
    }

    // Information about CLR grain interface (communication) types on local or remote silos.
    public class GrainInterfaceManager
    {
        public GrainInterfaceMetadata GetProperties(string interfaceTypeName) => null;

        public string GetInterfaceVersion(GrainInterfaceMetadata properties) => properties.Values["version"] ?? "random";
    }
}
