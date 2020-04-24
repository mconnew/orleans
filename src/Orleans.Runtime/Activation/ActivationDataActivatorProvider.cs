using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Internal;
using Orleans.Metadata;
using Orleans.Runtime.Placement;

namespace Orleans.Runtime
{
    internal class ActivationDataActivatorProvider : IGrainActivatorProvider
    {
        private readonly ConstructorArgumentFactory _argumentFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly PlacementStrategyResolver _placementStrategyResolver;
        private readonly IActivationCollector _activationCollector;
        private readonly SiloManifest _siloManifest;
        private readonly GrainClassMap _grainClassMap;
        private readonly GrainCollectionOptions _collectionOptions;
        private readonly IOptions<SiloMessagingOptions> _messagingOptions;
        private readonly TimeSpan _maxWarningRequestProcessingTime;
        private readonly TimeSpan _maxRequestProcessingTime;
        private readonly IRuntimeClient _runtimeClient;
        private readonly ILoggerFactory _loggerFactory;
        private IGrainRuntime _grainRuntime;

        public ActivationDataActivatorProvider(
            GrainClassMap grainClassMap,
            IServiceProvider serviceProvider,
            PlacementStrategyResolver placementStrategyResolver,
            IActivationCollector activationCollector,
            SiloManifest siloManifest,
            IOptions<SiloMessagingOptions> messagingOptions,
            IOptions<GrainCollectionOptions> collectionOptions,
            IRuntimeClient runtimeClient,
            ILoggerFactory loggerFactory)
        {
            _grainClassMap = grainClassMap;
            _argumentFactory = new ConstructorArgumentFactory(serviceProvider);
            _serviceProvider = serviceProvider;
            _placementStrategyResolver = placementStrategyResolver;
            _activationCollector = activationCollector;
            _siloManifest = siloManifest;
            _collectionOptions = collectionOptions.Value;
            _messagingOptions = messagingOptions;
            _maxWarningRequestProcessingTime = messagingOptions.Value.ResponseTimeout.Multiply(5);
            _maxRequestProcessingTime = messagingOptions.Value.MaxRequestProcessingTime;
            _runtimeClient = runtimeClient;
            _loggerFactory = loggerFactory;
        }

        public bool TryGet(GrainType grainType, out IGrainActivator activator)
        {
            if (!_grainClassMap.AvailableTypes.TryGetValue(grainType, out var grainClass)
                || !typeof(Grain).IsAssignableFrom(grainClass))
            {
                activator = default;
                return false;
            }

            // TODO: handle generics.
            string genericArguments = null;

            var argumentFactory = _argumentFactory.CreateFactory(grainClass);
            var createGrainInstance = ActivatorUtilities.CreateFactory(grainClass, argumentFactory.ArgumentTypes);

            var (activationCollector, collectionAgeLimit) = GetCollectionAgeLimit(grainType);

            activator = new ActivationDataActivator(
                createGrainInstance,
                _placementStrategyResolver.GetPlacementStrategy(grainType),
                activationCollector,
                collectionAgeLimit,
                _messagingOptions,
                _maxWarningRequestProcessingTime,
                _maxRequestProcessingTime,
                _runtimeClient,
                _loggerFactory,
                genericArguments,
                argumentFactory,
                _serviceProvider,
                _grainRuntime ??= _serviceProvider.GetRequiredService<IGrainRuntime>());
            return true;
        }

        private (IActivationCollector, TimeSpan) GetCollectionAgeLimit(GrainType grainType)
        {
            if (_siloManifest.Grains.TryGetValue(grainType, out var properties)
                && properties.Properties.TryGetValue(WellKnownGrainTypeProperties.IdleDeactivationPeriod, out var idleTimeoutString))
            {
                if (string.Equals(idleTimeoutString, WellKnownGrainTypeProperties.IndefiniteIdleDeactivationPeriodValue))
                {
                    return (null, default);
                }

                if (TimeSpan.TryParse(idleTimeoutString, out var result))
                {
                    return (_activationCollector, result);
                }
            }

            return (_activationCollector, _collectionOptions.CollectionAge);
        }

        private class ActivationDataActivator : IGrainActivator
        {
            private readonly ObjectFactory _createGrainInstance;
            private readonly PlacementStrategy _placementStrategy;
            private readonly IActivationCollector _activationCollector;
            private readonly TimeSpan _collectionAgeLimit;
            private readonly IOptions<SiloMessagingOptions> _messagingOptions;
            private readonly TimeSpan _maxWarningRequestProcessingTime;
            private readonly TimeSpan _maxRequestProcessingTime;
            private readonly IRuntimeClient _runtimeClient;
            private readonly ILoggerFactory _loggerFactory;
            private readonly string _genericArguments;
            private readonly ConstructorArgumentFactory.ArgumentFactory _argumentFactory;
            private readonly IServiceProvider _serviceProvider;
            private readonly IGrainRuntime _grainRuntime;

            public ActivationDataActivator(
                ObjectFactory createGrainInstance,
                PlacementStrategy placementStrategy,
                IActivationCollector activationCollector,
                TimeSpan collectionAgeLimit,
                IOptions<SiloMessagingOptions> messagingOptions,
                TimeSpan maxWarningRequestProcessingTime,
                TimeSpan maxRequestProcessingTime,
                IRuntimeClient runtimeClient,
                ILoggerFactory loggerFactory,
                string genericArguments,
                ConstructorArgumentFactory.ArgumentFactory argumentFactory,
                IServiceProvider serviceProvider,
                IGrainRuntime grainRuntime)
            {
                _createGrainInstance = createGrainInstance;
                _placementStrategy = placementStrategy;
                _activationCollector = activationCollector;
                _collectionAgeLimit = collectionAgeLimit;
                _messagingOptions = messagingOptions;
                _maxWarningRequestProcessingTime = maxWarningRequestProcessingTime;
                _maxRequestProcessingTime = maxRequestProcessingTime;
                _runtimeClient = runtimeClient;
                _loggerFactory = loggerFactory;
                _genericArguments = genericArguments;
                _argumentFactory = argumentFactory;
                _serviceProvider = serviceProvider;
                _grainRuntime = grainRuntime;
            }

            public IGrainContext CreateContext(ActivationAddress activationAddress)
            {
                var context = new ActivationData(
                    activationAddress,
                    _genericArguments,
                    _placementStrategy,
                    _activationCollector,
                    _collectionAgeLimit,
                    _messagingOptions,
                    _maxWarningRequestProcessingTime,
                    _maxRequestProcessingTime,
                    _runtimeClient,
                    _loggerFactory,
                    _serviceProvider,
                    _grainRuntime);

                RuntimeContext.SetExecutionContext(context, out var existingContext);

                try
                {
                    // Instantiate the grain itself
                    var grainInstance = (Grain)_createGrainInstance(_serviceProvider, _argumentFactory.CreateArguments(context));
                    context.SetGrainInstance(grainInstance);

                    (grainInstance as ILifecycleParticipant<IGrainLifecycle>)?.Participate(context.ObservableLifecycle);
                }
                finally
                {
                    RuntimeContext.SetExecutionContext(existingContext);
                }

                return context;
            }
        }
    }
}