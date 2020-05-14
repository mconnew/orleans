using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Internal;
using Orleans.Metadata;
using Orleans.Runtime;

namespace Orleans
{
    internal class GrainInterfaceToTypeResolverLifecycle<TLifecycle> : ILifecycleParticipant<TLifecycle>
        where TLifecycle : ILifecycleObservable
    {
        private readonly GrainInterfaceToTypeResolver _resolver;

        public GrainInterfaceToTypeResolverLifecycle(GrainInterfaceToTypeResolver resolver)
        {
            _resolver = resolver;
        }

        public void Participate(TLifecycle lifecycle)
        {
            lifecycle.Subscribe(nameof(GrainInterfaceToTypeResolver),
                ServiceLifecycleStage.RuntimeInitialize,
                ct => _resolver.StartAsync(ct),
                ct => _resolver.StopAsync(ct));
        }
    }

    public class GrainInterfaceToTypeResolver : IDisposable, IAsyncDisposable
    {
        private readonly object _lockObj = new object();
        private readonly IServiceProvider _serviceProvider;
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly ConcurrentDictionary<GrainInterfaceId, GrainType> _genericMapping = new ConcurrentDictionary<GrainInterfaceId, GrainType>();
        private IClusterManifestProvider _clusterManifestProvider;
        private Cache _cache;
        private Task _processUpdatesTask;

        public GrainInterfaceToTypeResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _cache = new Cache(MajorMinorVersion.Zero, new Dictionary<GrainInterfaceId, GrainType>(), new List<(string, GrainType)>());
        }

        public GrainType GetGrainTypeFromPrefix(string prefix)
        {
            var cache = _cache;
            foreach (var entry in cache.GrainTypePrefixes)
            {
                entry.Prefix.StartsWith(prefix);
                return entry.GrainType;
            }

            throw new KeyNotFoundException($"Could not find an implementation matching prefix \"{prefix}\"");
        }

        public GrainType GetGrainType(GrainInterfaceId interfaceId)
        {
            var cache = _cache;
            if (cache.Map.TryGetValue(interfaceId, out var result))
            {
                return result;
            }

            if (_genericMapping.TryGetValue(interfaceId, out result))
            {
                return result;
            }

            if (GenericGrainInterfaceId.TryParse(interfaceId, out var genericInterface))
            {
                var unconstructedInterface = genericInterface.GetGenericGrainType();
                var unconstructed = GetGrainType(unconstructedInterface.Value);
                if (GenericGrainType.TryParse(unconstructed, out var genericGrainType))
                {
                    if (genericGrainType.IsConstructed)
                    {
                        _genericMapping[interfaceId] = genericGrainType.GrainType;
                        return genericGrainType.GrainType;
                    }
                    else
                    {
                        var args = genericInterface.GetArgumentsString();
                        var constructed = GrainType.Create(genericGrainType.GrainType.ToStringUtf8() + args);
                        _genericMapping[interfaceId] = constructed;
                        return constructed;
                    }

                }

                _genericMapping[interfaceId] = unconstructed;
                return unconstructed;
            }

            if (_clusterManifestProvider is null)
            {
                EnsureInitialized();
                if (_clusterManifestProvider is object)
                {
                    return GetGrainType(interfaceId);
                }
            }

            throw new KeyNotFoundException($"Could not find an implementation for interface {interfaceId}");
        }

        private async Task ProcessClusterManifestUpdates()
        {
            await foreach (var _ in _clusterManifestProvider.Updates.WithCancellation(_cancellation.Token))
            {
                var newCache = BuildCache(_clusterManifestProvider.Current);
                Interlocked.Exchange(ref _cache, newCache);
            }
        }

        private void EnsureInitialized()
        {
            if (_processUpdatesTask is object) return;
            lock (_lockObj)
            {
                _clusterManifestProvider = _serviceProvider.GetRequiredService<IClusterManifestProvider>();
                _cache = BuildCache(_clusterManifestProvider.Current);
                _processUpdatesTask = Task.Run(ProcessClusterManifestUpdates);
            }
        }

        public Task StartAsync(CancellationToken cancellation)
        {
            EnsureInitialized();
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellation)
        {
            _cancellation.Cancel();
            if (cancellation.IsCancellationRequested) return;
            if (_processUpdatesTask is Task task) await Task.WhenAny(task, cancellation.WhenCancelled());
        }

        private static Cache BuildCache(ClusterManifest clusterManifest)
        {
            var result = new Dictionary<GrainInterfaceId, GrainType>();
            var prefixes = new List<(string, GrainType)>();

            foreach (var entry in clusterManifest.Silos)
            {
                var silo = entry.Key;
                var manifest = entry.Value;
                foreach (var grainInterface in manifest.Interfaces)
                {
                    var id = grainInterface.Key;

                    if (grainInterface.Value.Properties.TryGetValue(WellKnownGrainInterfaceProperties.DefaultGrainType, out var defaultTypeString))
                    {
                        result[id] = GrainType.Create(defaultTypeString);
                        continue;
                    } 
                }

                foreach (var grainType in manifest.Grains)
                {
                    var id = grainType.Key;
                    foreach (var implemented in SupportedGrainInterfaces(grainType.Value))
                    {
                        if (!result.ContainsKey(implemented))
                        {
                            result[implemented] = id;
                        }
                    }

                    if (grainType.Value.Properties.TryGetValue(WellKnownGrainTypeProperties.GrainTypePrefix, out var typeNamePrefix))
                    {
                        prefixes.Add((typeNamePrefix, id));
                    }
                }
            }

            return new Cache(clusterManifest.Version, result, prefixes);

            IEnumerable<GrainInterfaceId> SupportedGrainInterfaces(GrainProperties grain)
            {
                foreach (var property in grain.Properties)
                {
                    if (property.Key.StartsWith(WellKnownGrainTypeProperties.ImplementedInterfacePrefix))
                    {
                        yield return GrainInterfaceId.Create(property.Value);
                    }
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await this.StopAsync(CancellationToken.None);
        }

        public void Dispose()
        {
            _cancellation.Cancel();
        }

        private class Cache
        {
            public Cache(MajorMinorVersion version, Dictionary<GrainInterfaceId, GrainType> map, List<(string, GrainType)> prefixes)
            {
                this.Version = version;
                this.Map = map;
                this.GrainTypePrefixes = prefixes;
            }

            public MajorMinorVersion Version { get; }
            public Dictionary<GrainInterfaceId, GrainType> Map { get; }
            public List<(string Prefix, GrainType GrainType)> GrainTypePrefixes { get; }
        }
    }
}
