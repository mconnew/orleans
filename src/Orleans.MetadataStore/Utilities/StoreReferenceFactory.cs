using System;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;

namespace Orleans.MetadataStore
{
    internal class StoreReferenceFactory : IStoreReferenceFactory
    {
        private IGrainFactory _grainFactory;
        private readonly ILocalSiloDetails _localSiloDetails;
        private readonly IServiceProvider _serviceProvider;
        private LocalMetadataStore _localMetadataStore;

        public StoreReferenceFactory(ILocalSiloDetails localSiloDetails, IServiceProvider services)
        {
            _localSiloDetails = localSiloDetails;
            _serviceProvider = services;
        }

        public SiloAddress GetAddress(IRemoteMetadataStore remoteStore)
        {
            if (remoteStore is GrainReference grainReference) return FixedPlacement.ParseSiloAddress(grainReference.GrainId);
            if (remoteStore is LocalMetadataStore) return _localSiloDetails.SiloAddress;
            throw new InvalidOperationException($"Object of type {remoteStore?.GetType()?.ToString() ?? "null"} is not an instance of type {typeof(GrainReference)}");
        }

        public IRemoteMetadataStore GetReference(SiloAddress address, short instanceNum)
        {
            if (address.Endpoint.Equals(_localSiloDetails.SiloAddress.Endpoint))
            {
                return _localMetadataStore ??= ActivatorUtilities.CreateInstance<LocalMetadataStore>(_serviceProvider);
            }

            var grainId = FixedPlacement.CreateGrainId(RemoteMetadataStoreGrain.GrainType, address, "0"/*instancenum.tostring()*/);
            if (_grainFactory is null)
            {
                _grainFactory = _serviceProvider.GetRequiredService<IGrainFactory>();
            }

            return _grainFactory.GetGrain<IRemoteMetadataStoreGrain>(grainId);
        }
    }
}