using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Orleans.MetadataStore.Storage;
using Orleans.Runtime;

namespace Orleans.MetadataStore
{
    internal class StoreReferenceFactory : IStoreReferenceFactory
    {
        private readonly IInternalGrainFactory grainFactory;
        private readonly ILocalSiloDetails localSiloDetails;
        private readonly IServiceProvider serviceProvider;

        public StoreReferenceFactory(IInternalGrainFactory grainFactory, ILocalSiloDetails localSiloDetails, IServiceProvider serviceProvider)
        {
            this.grainFactory = grainFactory;
            this.localSiloDetails = localSiloDetails;
            this.serviceProvider = serviceProvider;
        }

        public SiloAddress GetAddress(IRemoteMetadataStore remoteStore)
        {
            if (remoteStore is GrainReference grainReference) return grainReference.SystemTargetSilo;
            if (remoteStore is LocalMetadataStore) return this.localSiloDetails.SiloAddress;
            throw new InvalidOperationException($"Object of type {remoteStore?.GetType()?.ToString() ?? "null"} is not an instance of type {typeof(GrainReference)}");
        }

        public IRemoteMetadataStore GetReference(SiloAddress address, short instanceNum)
        {
            if (address.Endpoint.Equals(this.localSiloDetails.SiloAddress.Endpoint))
            {
                return ActivatorUtilities.CreateInstance<LocalMetadataStore>(this.serviceProvider);
            }

            var grainId = GrainId.GetGrainServiceGrainId(instanceNum, Constants.KeyValueStoreSystemTargetTypeCode);
            return this.grainFactory.GetSystemTarget<IRemoteMetadataStore>(grainId, address);
        }

        internal class LocalMetadataStore : IRemoteMetadataStore
        {
            private readonly MetadataStoreManager manager;
            private readonly ILocalStore store;

            public LocalMetadataStore(
                MetadataStoreManager manager,
                ILocalStore store)
            {
                this.manager = manager;
                this.store = store;
            }

            public ValueTask<PrepareResponse> Prepare(string key, Ballot proposerParentBallot, Ballot ballot) => this.manager.Prepare(key, proposerParentBallot, ballot);

            public ValueTask<AcceptResponse> Accept(string key, Ballot proposerParentBallot, Ballot ballot, object value) => this.manager.Accept(key, proposerParentBallot, ballot, value);

            public ValueTask<List<string>> GetKeys() => this.store.GetKeys(int.MaxValue);
        }
    }
}