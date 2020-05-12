using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans.Internal;
using Orleans.Metadata;
using Orleans.Runtime;

namespace Orleans
{
    public class GrainInterfaceToTypeResolver : IDisposable, IAsyncDisposable
    {
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly ConcurrentDictionary<GrainInterfaceId, GrainType> _genericMapping = new ConcurrentDictionary<GrainInterfaceId, GrainType>();
        private readonly IClusterManifestProvider _clusterManifestProvider;
        private Dictionary<GrainInterfaceId, GrainType> _cache;
        private Task _processUpdatesTask;

        public GrainInterfaceToTypeResolver(IClusterManifestProvider clusterManifestProvider)
        {
            _clusterManifestProvider = clusterManifestProvider;
            _cache = BuildCache(clusterManifestProvider.Current);
            _ = StartAsync();
        }

        public GrainType GetGrainType(GrainInterfaceId interfaceId)
        {
            var cache = _cache;
            if (cache.TryGetValue(interfaceId, out var result))
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
                        var str = unconstructedInterface.GetArgumentsString();
                        var constructed = GrainType.Create(genericGrainType.GrainType.ToStringUtf8() + str);
                        _genericMapping[interfaceId] = constructed;
                        return constructed;
                    }

                }

                _genericMapping[interfaceId] = unconstructed;
                return unconstructed;
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

        public Task StartAsync()
        {
            if (_processUpdatesTask is object) throw new InvalidOperationException("This instance has already been started.");
            _processUpdatesTask = Task.Run(ProcessClusterManifestUpdates);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellation)
        {
            _cancellation.Cancel();
            if (cancellation.IsCancellationRequested) return;
            if (_processUpdatesTask is Task task) await Task.WhenAny(task, cancellation.WhenCancelled());
        }

        private static Dictionary<GrainInterfaceId, GrainType> BuildCache(ClusterManifest clusterManifest)
        {
            var result = new Dictionary<GrainInterfaceId, GrainType>();

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
                }
            }

            return result;

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
    }
}
