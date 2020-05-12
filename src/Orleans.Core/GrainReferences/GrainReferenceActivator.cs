
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Orleans.ApplicationParts;
using Orleans.Metadata;
using Orleans.Runtime;

namespace Orleans.GrainReferences
{
    /// <summary>
    /// The central point for creating <see cref="GrainReference"/> instances.
    /// </summary>
    public sealed class GrainReferenceActivator
    {
        private readonly object _lockObj = new object();
        private readonly IServiceProvider _serviceProvider;
        private readonly IGrainReferenceActivatorProvider[] _providers;
        private Dictionary<(GrainType, GrainInterfaceId), Entry> _activators = new Dictionary<(GrainType, GrainInterfaceId), Entry>();

        public GrainReferenceActivator(
            IServiceProvider serviceProvider,
            IEnumerable<IGrainReferenceActivatorProvider> providers)
        {
            _serviceProvider = serviceProvider;
            _providers = providers.ToArray();
        }

        public GrainReference CreateReference(GrainId grainId, GrainInterfaceId interfaceId)
        {
            if (!_activators.TryGetValue((grainId.Type, interfaceId), out var entry))
            {
                entry = CreateActivator(grainId.Type, interfaceId);
            }

            var result = entry.Activator.CreateReference(grainId);
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

                    entry = new Entry(activator);
                    _activators = new Dictionary<(GrainType, GrainInterfaceId), Entry>(_activators) { [(grainType, interfaceId)] = entry };
                }

                return entry;
            }
        }

        private readonly struct Entry
        {
            public Entry(IGrainReferenceActivator activator)
            {
                this.Activator = activator;
            }

            public IGrainReferenceActivator Activator { get; }
        }
    }

    internal class ImrGrainReferenceActivatorProvider : IGrainReferenceActivatorProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TypeConverter _typeConverter;
        private readonly Dictionary<GrainInterfaceId, Type> _mapping;
        private IGrainReferenceRuntime _grainReferenceRuntime;

        public ImrGrainReferenceActivatorProvider(
            IServiceProvider serviceProvider,
            IApplicationPartManager appParts,
            GrainInterfaceIdResolver resolver,
            TypeConverter typeConverter)
        {
            _serviceProvider = serviceProvider;
            this._typeConverter = typeConverter;
            var interfaces = appParts.CreateAndPopulateFeature<GrainInterfaceFeature>();
            _mapping = new Dictionary<GrainInterfaceId, Type>();
            foreach (var @interface in interfaces.Interfaces)
            {
                var id = resolver.GetGrainInterfaceId(@interface.InterfaceType);
                _mapping[id] = @interface.ReferenceType;
            }
        }

        public bool TryGet(GrainType grainType, GrainInterfaceId interfaceId, out IGrainReferenceActivator activator)
        {
            GrainInterfaceId lookupId;
            Type[] args;
            if (GenericGrainInterfaceId.TryParse(interfaceId, out var genericId))
            {
                lookupId = genericId.Value;
                args = genericId.GetArguments(_typeConverter);
            }
            else
            {
                lookupId = interfaceId;
                args = default;
            }

            if (!_mapping.TryGetValue(lookupId, out var referenceType))
            {
                activator = default;
                return false;
            }

            if (args is Type[])
            {
                referenceType = referenceType.MakeGenericType(args);
            }

            var runtime = _grainReferenceRuntime ??= _serviceProvider.GetRequiredService<IGrainReferenceRuntime>();
            var shared = new GrainReferenceShared(grainType, interfaceId, runtime);

            activator = new ImrGrainReferenceActivator(referenceType, shared);
            return true;
        }

        private class ImrGrainReferenceActivator : IGrainReferenceActivator
        {
            private readonly Type _referenceType;
            private readonly GrainReferenceShared _shared;

            public ImrGrainReferenceActivator(Type referenceType, GrainReferenceShared shared)
            {
                _referenceType = referenceType;
                _shared = shared;
            }

            public GrainReference CreateReference(GrainId grainId)
            {
                return (GrainReference)Activator.CreateInstance(
                    type: _referenceType,
                    bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic,
                    binder: null,
                    args: new object[] { _shared, grainId.Key },
                    culture: CultureInfo.InvariantCulture);
            }
        }
    }

    public interface IGrainReferenceActivatorProvider
    {
        bool TryGet(GrainType grainType, GrainInterfaceId interfaceId, out IGrainReferenceActivator activator);
    }

    public interface IGrainReferenceActivator
    {
        public GrainReference CreateReference(GrainId grainId);
    }
}
