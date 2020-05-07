
using System;
using System.Collections.Generic;
using System.Linq;
using Orleans.Runtime;

namespace Orleans.GrainReferences
{
    /// <summary>
    /// The central point for creating <see cref="GrainReference"/> instances.
    /// </summary>
    public sealed class GrainReferenceActivator
    {
        private readonly object _lockObj = new object();
        private readonly IGrainReferenceRuntime _grainReferenceRuntime;
        private readonly IGrainReferenceActivatorProvider[] _providers;
        private Dictionary<(GrainType, GrainInterfaceId), Entry> _activators = new Dictionary<(GrainType, GrainInterfaceId), Entry>();

        public GrainReferenceActivator(
            IGrainReferenceRuntime grainReferenceRuntime,
            IEnumerable<IGrainReferenceActivatorProvider> providers)
        {
            _grainReferenceRuntime = grainReferenceRuntime;
            _providers = providers.ToArray();
        }

        public GrainReference CreateReference(GrainId grainId, GrainInterfaceId interfaceId)
        {
            if (!_activators.TryGetValue((grainId.Type, interfaceId), out var entry))
            {
                entry = CreateActivator(grainId.Type, interfaceId);
            }

            var result = entry.Activator.CreateReference(entry.Prototype, grainId);
            return result;
        }

        private Entry CreateActivator(GrainType grainType, GrainInterfaceId interfaceId)
        {
            lock (_lockObj)
            {
                if (!_activators.TryGetValue((grainType, interfaceId), out var entry))
                {
                    IGrainReferenceActivator activator = null;
                    foreach (var provider in _providers)
                    {
                        if (provider.TryGet(grainType, interfaceId, out activator))
                        {
                            break;
                        }
                    }

                    if (activator is null)
                    {
                        throw new InvalidOperationException($"Unable to find an {nameof(IGrainReferenceActivatorProvider)} for grain type {grainType}");
                    }

                    var prototype = new GrainReferenceShared(grainType, interfaceId, _grainReferenceRuntime);
                    entry = new Entry(prototype, activator);
                    _activators = new Dictionary<(GrainType, GrainInterfaceId), Entry>(_activators) { [(grainType, interfaceId)] = entry };
                }

                return entry;
            }
        }

        private readonly struct Entry
        {
            public Entry(GrainReferenceShared prototype, IGrainReferenceActivator activator)
            {
                this.Prototype = prototype;
                this.Activator = activator;
            }

            public GrainReferenceShared Prototype { get; }

            public IGrainReferenceActivator Activator { get; }
        }
    }

    public interface IGrainReferenceActivatorProvider
    {
        bool TryGet(GrainType grainType, GrainInterfaceId interfaceId, out IGrainReferenceActivator activator);
    }

    public interface IGrainReferenceActivator
    {
        public GrainReference CreateReference(
            GrainReferenceShared prototype,
            GrainId grainId);
    }
}