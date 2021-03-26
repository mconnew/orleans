using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Hagar;
using Hagar.Configuration;
using Hagar.Serializers;
using Hagar.TypeSystem;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans.ApplicationParts;
using Orleans.Configuration;
using Orleans.Configuration.Internal;
using Orleans.Configuration.Validators;
using Orleans.GrainReferences;
using Orleans.Messaging;
using Orleans.Metadata;
using Orleans.Networking.Shared;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Runtime.Messaging;
using Orleans.Runtime.Versions;
using Orleans.Serialization;
using Orleans.Statistics;

namespace Orleans
{
    internal static class DefaultClientServices
    {
        public static void AddDefaultServices(IServiceCollection services)
        {
            // Options logging
            services.TryAddSingleton(typeof(IOptionFormatter<>), typeof(DefaultOptionsFormatter<>));
            services.TryAddSingleton(typeof(IOptionFormatterResolver<>), typeof(DefaultOptionsFormatterResolver<>));

            services.AddSingleton<ClientOptionsLogger>();
            services.AddFromExisting<ILifecycleParticipant<IClusterClientLifecycle>, ClientOptionsLogger>();
            services.TryAddSingleton<TelemetryManager>();
            services.TryAddFromExisting<ITelemetryProducer, TelemetryManager>();
            services.TryAddSingleton<IHostEnvironmentStatistics, NoOpHostEnvironmentStatistics>();
            services.TryAddSingleton<IAppEnvironmentStatistics, AppEnvironmentStatistics>();
            services.TryAddSingleton<ClientStatisticsManager>();
            services.TryAddSingleton<ApplicationRequestsStatisticsGroup>();
            services.TryAddSingleton<StageAnalysisStatisticsGroup>();
            services.TryAddSingleton<SchedulerStatisticsGroup>();
            services.TryAddSingleton<SerializationStatisticsGroup>();
            services.AddLogging();
            services.TryAddSingleton<GrainBindingsResolver>();
            services.TryAddSingleton<OutsideRuntimeClient>();
            services.TryAddSingleton<ClientGrainContext>();
            services.AddFromExisting<IGrainContextAccessor, ClientGrainContext>();
            services.TryAddFromExisting<IRuntimeClient, OutsideRuntimeClient>();
            services.TryAddFromExisting<IClusterConnectionStatusListener, OutsideRuntimeClient>();
            services.TryAddSingleton<GrainFactory>();
            services.TryAddSingleton<GrainInterfaceTypeToGrainTypeResolver>();
            services.TryAddSingleton<GrainReferenceActivator>();
            services.AddSingleton<IGrainReferenceActivatorProvider, NewGrainReferenceActivatorProvider>();
            services.AddSingleton<IGrainReferenceActivatorProvider, ImrGrainReferenceActivatorProvider>();
            services.AddSingleton<IGrainReferenceActivatorProvider, UntypedGrainReferenceActivatorProvider>();
            services.TryAddSingleton<NewRpcProvider>();
            services.TryAddSingleton<ImrRpcProvider>();
            services.TryAddSingleton<ImrGrainMethodInvokerProvider>();
            services.TryAddSingleton<GrainReferenceSerializer>();
            services.TryAddSingleton<GrainReferenceKeyStringConverter>();
            services.TryAddSingleton<IGrainReferenceRuntime, GrainReferenceRuntime>();
            services.TryAddSingleton<GrainInterfaceTypeResolver>();
            services.TryAddSingleton<GrainPropertiesResolver>();
            services.TryAddSingleton<Orleans.Runtime.TypeConverter>();
            services.TryAddSingleton<IGrainCancellationTokenRuntime, GrainCancellationTokenRuntime>();
            services.TryAddFromExisting<IGrainFactory, GrainFactory>();
            services.TryAddFromExisting<IInternalGrainFactory, GrainFactory>();
            services.TryAddSingleton<ClientProviderRuntime>();
            services.TryAddSingleton<MessageFactory>();
            services.TryAddFromExisting<IProviderRuntime, ClientProviderRuntime>();
            services.TryAddSingleton<IInternalClusterClient, ClusterClient>();
            services.TryAddFromExisting<IClusterClient, IInternalClusterClient>();

            // Serialization
            services.TryAddSingleton<SerializationManager>(sp => ActivatorUtilities.CreateInstance<SerializationManager>(sp,
                sp.GetRequiredService<IOptions<ClientMessagingOptions>>().Value.LargeMessageWarningThreshold));
            services.TryAddSingleton<ITypeResolver, Orleans.Runtime.CachedTypeResolver>();
            services.TryAddSingleton<IFieldUtils, FieldUtils>();

            // Register the ISerializable serializer first, so that it takes precedence
            services.AddSingleton<DotNetSerializableSerializer>();
            services.AddFromExisting<IKeyedSerializer, DotNetSerializableSerializer>();

            services.TryAddSingleton<ILBasedSerializer>();
            services.AddFromExisting<IKeyedSerializer, ILBasedSerializer>();

            // Application parts
            var parts = services.GetApplicationPartManager();
            services.TryAddSingleton<IApplicationPartManager>(parts);
            parts.AddApplicationPart(new AssemblyPart(typeof(RuntimeVersion).Assembly) { IsFrameworkAssembly = true });
            parts.AddFeatureProvider(new BuiltInTypesSerializationFeaturePopulator());
            parts.AddFeatureProvider(new AssemblyAttributeFeatureProvider<GrainInterfaceFeature>());
            parts.AddFeatureProvider(new AssemblyAttributeFeatureProvider<SerializerFeature>());
            services.AddTransient<IConfigurationValidator, ApplicationPartValidator>();

            services.TryAddSingleton(typeof(IKeyedServiceCollection<,>), typeof(KeyedServiceCollection<,>));

            // Add default option formatter if none is configured, for options which are required to be configured 
            services.ConfigureFormatter<ClusterOptions>();
            services.ConfigureFormatter<ClientMessagingOptions>();
            services.ConfigureFormatter<ConnectionOptions>();
            services.ConfigureFormatter<StatisticsOptions>();

            services.AddTransient<IConfigurationValidator, ClusterOptionsValidator>();
            services.AddTransient<IConfigurationValidator, ClientClusteringValidator>();

            // TODO: abstract or move into some options.
            services.AddSingleton<SocketSchedulers>();
            services.AddSingleton<SharedMemoryPool>();

            // Networking
            services.TryAddSingleton<ConnectionCommon>();
            services.TryAddSingleton<ConnectionManager>();
            services.AddSingleton<ILifecycleParticipant<IClusterClientLifecycle>, ConnectionManagerLifecycleAdapter<IClusterClientLifecycle>>();

            services.AddSingletonKeyedService<object, IConnectionFactory>(
                ClientOutboundConnectionFactory.ServicesKey,
                (sp, key) => ActivatorUtilities.CreateInstance<SocketConnectionFactory>(sp));

#if true
            services.AddHagar();
            services.AddSingleton<ITypeFilter, AllowOrleansTypes>();
            services.AddSingleton<ISpecializableCodec, GrainReferenceCodecProvider>();
            services.AddSingleton<Hagar.Cloning.IGeneralizedCopier, GrainReferenceCopier>();

            services.TryAddTransient<IMessageSerializer>(sp => ActivatorUtilities.CreateInstance<HagarMessageSerializer>(
                sp,
                sp.GetRequiredService<IOptions<ClientMessagingOptions>>().Value.MaxMessageHeaderSize,
                sp.GetRequiredService<IOptions<ClientMessagingOptions>>().Value.MaxMessageBodySize));
#else
            services.TryAddTransient<IMessageSerializer>(sp => ActivatorUtilities.CreateInstance<MessageSerializer>(
                sp,
                sp.GetRequiredService<IOptions<ClientMessagingOptions>>().Value.MaxMessageHeaderSize,
                sp.GetRequiredService<IOptions<ClientMessagingOptions>>().Value.MaxMessageBodySize));
#endif
            services.TryAddSingleton<ConnectionFactory, ClientOutboundConnectionFactory>();
            services.TryAddSingleton<ClientMessageCenter>(sp => sp.GetRequiredService<OutsideRuntimeClient>().MessageCenter);
            services.TryAddFromExisting<IMessageCenter, ClientMessageCenter>();
            services.AddSingleton<GatewayManager>();
            services.AddSingleton<NetworkingTrace>();
            services.AddSingleton<MessagingTrace>();

            // Type metadata
            services.AddSingleton<ClientClusterManifestProvider>();
            services.AddFromExisting<IClusterManifestProvider, ClientClusterManifestProvider>();
            services.AddSingleton<ClientManifestProvider>();
            services.AddSingleton<IGrainInterfaceTypeProvider, AttributeGrainInterfaceTypeProvider>();
            services.AddSingleton<GrainTypeResolver>();
            services.AddSingleton<IGrainTypeProvider, AttributeGrainTypeProvider>();
            services.AddSingleton<IGrainTypeProvider, LegacyGrainTypeResolver>();
            services.AddSingleton<GrainPropertiesResolver>();
            services.AddSingleton<GrainVersionManifest>();
            services.AddSingleton<GrainInterfaceTypeResolver>();
            services.AddSingleton<IGrainInterfacePropertiesProvider, AttributeGrainInterfacePropertiesProvider>();
            services.AddSingleton<IGrainPropertiesProvider, AttributeGrainPropertiesProvider>();
            services.AddSingleton<IGrainInterfacePropertiesProvider, TypeNameGrainPropertiesProvider>();
            services.AddSingleton<IGrainPropertiesProvider, TypeNameGrainPropertiesProvider>();
            services.AddSingleton<IGrainPropertiesProvider, ImplementedInterfaceProvider>();
            // Logging helpers
            services.AddSingleton<ClientLoggingHelper>();
            services.AddFromExisting<ILifecycleParticipant<IClusterClientLifecycle>, ClientLoggingHelper>();
            services.AddFromExisting<IGrainIdLoggingHelper, ClientLoggingHelper>();
            services.AddFromExisting<IInvokeMethodRequestLoggingHelper, ClientLoggingHelper>();
        }

        private class AllowOrleansTypes : ITypeFilter
        {
            public bool? IsTypeNameAllowed(string typeName, string assemblyName)
            {
                if (assemblyName is { Length: > 0} && assemblyName.Contains("Orleans"))
                {
                    return true;
                }

                return null;
            }
        }
    }
}
