using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.ApplicationParts;
using Orleans.Configuration;
using Orleans.Metadata;

namespace Orleans.Runtime
{
    /// <summary>
    /// The central point for creating grain activations.
    /// </summary>
    public sealed class GrainActivator
    {
        private readonly object lockObj = new object();
        private readonly IGrainActivatorProvider[] providers;
        private readonly IConfigureGrain[] configureContextActions;
        private ImmutableDictionary<GrainType, ActivatorEntry> activators = ImmutableDictionary<GrainType, ActivatorEntry>.Empty;

        public GrainActivator(
            IEnumerable<IGrainActivatorProvider> providers,
            IEnumerable<IConfigureGrain> configureContextActions)
        {
            this.providers = providers.ToArray();
            this.configureContextActions = configureContextActions.ToArray();
        }

        public IGrainContext CreateInstance(ActivationAddress address)
        {
            var grainId = address.Grain;
            if (!this.activators.TryGetValue(grainId.Type, out var activator))
            {
                activator = this.CreateActivator(grainId.Type);
            }

            var result = activator.Activator.CreateContext(address);
            foreach (var configure in activator.ConfigureActions)
            {
                configure.Configure(result);
            }

            return result;
        }

        private ActivatorEntry CreateActivator(GrainType grainType)
        {
            lock (this.lockObj)
            {
                if (!this.activators.TryGetValue(grainType, out var configuredActivator))
                {
                    IGrainActivator unconfiguredActivator = null;
                    foreach (var provider in this.providers)
                    {
                        if (provider.TryGet(grainType, out unconfiguredActivator))
                        {
                            break;
                        }
                    }

                    if (unconfiguredActivator is null)
                    {
                        throw new InvalidOperationException($"Unable to find an {nameof(IGrainActivatorProvider)} for grain type {grainType}");
                    }

                    var applicableConfigureActions = this.configureContextActions.Where(c => c.CanConfigure(grainType)).ToArray();
                    configuredActivator = new ActivatorEntry(unconfiguredActivator, applicableConfigureActions);
                    this.activators = this.activators.SetItem(grainType, configuredActivator);
                }

                return configuredActivator;
            }
        }

        private readonly struct ActivatorEntry
        {
            public ActivatorEntry(
                IGrainActivator activator,
                IConfigureGrain[] configureActions)
            {
                this.Activator = activator;
                this.ConfigureActions = configureActions;
            }

            public IGrainActivator Activator { get; }

            public IConfigureGrain[] ConfigureActions { get; }
        }
    }

    public interface IGrainActivatorProvider
    {
        bool TryGet(GrainType grainType, out IGrainActivator activator);
    }

    public interface IGrainActivator
    {
        public IGrainContext CreateContext(ActivationAddress grainId);
    }

    public interface IConfigureGrain
    {
        bool CanConfigure(GrainType grainType);
        void Configure(IGrainContext context);
    }
}