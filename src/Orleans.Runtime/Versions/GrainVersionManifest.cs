using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans.Internal;
using Orleans.Metadata;

namespace Orleans.Runtime.Versions
{
    internal class GrainVersionManifest : ILifecycleParticipant<ISiloLifecycle>
    {
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly ConcurrentDictionary<GrainInterfaceId, GrainInterfaceId> _genericInterfaceMapping = new ConcurrentDictionary<GrainInterfaceId, GrainInterfaceId>();
        private readonly ConcurrentDictionary<GrainType, GrainType> _genericGrainTypeMapping = new ConcurrentDictionary<GrainType, GrainType>();
        private readonly IClusterManifestProvider _clusterManifestProvider;
        private CachedVersionMap _cache;
        private Task _processUpdatesTask;

        public GrainVersionManifest(IClusterManifestProvider clusterManifestProvider)
        {
            _clusterManifestProvider = clusterManifestProvider;
            _cache = BuildCachedVersionMap(clusterManifestProvider.Current);
        }

        public ushort[] GetAvailableVersions(GrainInterfaceId interfaceId)
        {
            var cache = _cache;
            if (cache.AvailableVersions.TryGetValue(interfaceId, out var result))
            {
                return result;
            }

            if (_genericInterfaceMapping.TryGetValue(interfaceId, out var genericInterfaceId))
            {
                return GetAvailableVersions(genericInterfaceId);
            }

            if (GenericGrainInterfaceId.TryParse(interfaceId, out var generic) && generic.IsConstructed)
            {
                var genericId = _genericInterfaceMapping[interfaceId] = generic.GetGenericGrainType().Value;
                return GetAvailableVersions(genericId);
            }

            // No versions available.
            return Array.Empty<ushort>();
        }

        public SiloAddress[] GetSupportedSilos(GrainInterfaceId interfaceId, ushort version)
        {
            var cache = _cache;
            if (cache.SupportedSilosByInterface.TryGetValue((interfaceId, version), out var result))
            {
                return result;
            }

            if (_genericInterfaceMapping.TryGetValue(interfaceId, out var genericInterfaceId))
            {
                return GetSupportedSilos(genericInterfaceId, version);
            }

            if (GenericGrainInterfaceId.TryParse(interfaceId, out var generic) && generic.IsConstructed)
            {
                var genericId = _genericInterfaceMapping[interfaceId] = generic.GetGenericGrainType().Value;
                return GetSupportedSilos(genericId, version);
            }

            // No supported silos for this version.
            return Array.Empty<SiloAddress>();
        }

        public SiloAddress[] GetSupportedSilos(GrainType grainType)
        {
            var cache = _cache;
            if (cache.SupportedSilosByGrainType.TryGetValue(grainType, out var result))
            {
                return result;
            }

            if (_genericGrainTypeMapping.TryGetValue(grainType, out var genericGrainType))
            {
                return GetSupportedSilos(genericGrainType);
            }

            if (GenericGrainType.TryParse(grainType, out var generic) && generic.IsConstructed)
            {
                var genericId = _genericGrainTypeMapping[grainType] = generic.GetGenericGrainType().GrainType;
                return GetSupportedSilos(genericId);
            }

            // No supported silos for this type.
            return Array.Empty<SiloAddress>();
        }

        public Dictionary<ushort, SiloAddress[]> GetSupportedSilos(GrainType grainType, GrainInterfaceId interfaceId, ushort[] versions)
        {
            var result = new Dictionary<ushort, SiloAddress[]>();
            foreach (var version in versions)
            {
                var silosWithGrain = this.GetSupportedSilos(grainType);

                // We need to sort this so the list of silos returned will
                // be the same across all silos in the cluster
                var silosWithCorrectVersion = this.GetSupportedSilos(interfaceId, version)
                    .Intersect(silosWithGrain)
                    .OrderBy(addr => addr)
                    .ToArray();
                result[version] = silosWithCorrectVersion;
            }

            return result;
        }

        private async Task ProcessClusterManifestUpdates()
        {
            await foreach (var _ in _clusterManifestProvider.Updates.WithCancellation(_cancellation.Token))
            {
                var newCache = BuildCachedVersionMap(_clusterManifestProvider.Current);
                Interlocked.Exchange(ref _cache, newCache);
            }
        }

        void ILifecycleParticipant<ISiloLifecycle>.Participate(ISiloLifecycle lifecycle)
        {
            lifecycle.Subscribe(
                nameof(GrainVersionManifest),
                ServiceLifecycleStage.RuntimeInitialize,
                _ =>
                {
                    _processUpdatesTask = Task.Run(ProcessClusterManifestUpdates);
                    return Task.CompletedTask;
                },
                async cancellation =>
                {
                    _cancellation.Cancel();
                    if (cancellation.IsCancellationRequested) return;

                    if (_processUpdatesTask is Task task) await Task.WhenAny(task, cancellation.WhenCancelled());
                });
        }

        private static CachedVersionMap BuildCachedVersionMap(ClusterManifest clusterManifest)
        {
            var available = new Dictionary<GrainInterfaceId, List<ushort>>();
            var supportedInterfaces = new Dictionary<(GrainInterfaceId, ushort), List<SiloAddress>>();
            var supportedGrains = new Dictionary<GrainType, List<SiloAddress>>();

            foreach (var entry in clusterManifest.Silos)
            {
                var silo = entry.Key;
                var manifest = entry.Value;
                foreach (var grainInterface in manifest.Interfaces)
                {
                    var id = grainInterface.Key;

                    if (!grainInterface.Value.Properties.TryGetValue(WellKnownGrainInterfaceProperties.Version, out var versionString)
                        || !ushort.TryParse(versionString, out var version))
                    {
                        version = 0;
                    }

                    if (!available.TryGetValue(id, out var versions))
                    {
                        available[id] = new List<ushort> { version };
                    }
                    else if (!versions.Contains(version))
                    {
                        versions.Add(version);
                    }

                    if (!supportedInterfaces.TryGetValue((id, version), out var supportedSilos))
                    {
                        supportedInterfaces[(id, version)] = new List<SiloAddress> { silo };
                    }
                    else if (!supportedSilos.Contains(silo))
                    {
                        supportedSilos.Add(silo);
                    }
                }

                foreach (var grainType in manifest.Grains)
                {
                    var id = grainType.Key;
                    if (!supportedGrains.TryGetValue(id, out var supportedSilos))
                    {
                        supportedGrains[id] = new List<SiloAddress> { silo };
                    }
                    else if (!supportedSilos.Contains(silo))
                    {
                        supportedSilos.Add(silo);
                    }
                }
            }

            var result = new CachedVersionMap();
            var resultAvailable = result.AvailableVersions;
            foreach (var entry in available)
            {
                entry.Value.Sort();
                resultAvailable[entry.Key] = entry.Value.ToArray();
            }

            foreach (var entry in supportedInterfaces)
            {
                entry.Value.Sort();
                result.SupportedSilosByInterface[entry.Key] = entry.Value.ToArray();
            }

            foreach (var entry in supportedGrains)
            {
                entry.Value.Sort();
                result.SupportedSilosByGrainType[entry.Key] = entry.Value.ToArray();
            }

            return result;
        }

        private class CachedVersionMap
        {
            public Dictionary<GrainInterfaceId, ushort[]> AvailableVersions { get; } = new Dictionary<GrainInterfaceId, ushort[]>();
            public Dictionary<(GrainInterfaceId, ushort), SiloAddress[]> SupportedSilosByInterface { get; } = new Dictionary<(GrainInterfaceId, ushort), SiloAddress[]>();
            public Dictionary<GrainType, SiloAddress[]> SupportedSilosByGrainType { get; } = new Dictionary<GrainType, SiloAddress[]>();
        }
    }
}