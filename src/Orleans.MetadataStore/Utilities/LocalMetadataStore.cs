using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.MetadataStore.Storage;

namespace Orleans.MetadataStore
{
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

        public ValueTask<PrepareResponse> Prepare(string key, Ballot proposerParentBallot, Ballot ballot) => manager.Prepare(key, proposerParentBallot, ballot);

        public ValueTask<AcceptResponse> Accept(string key, Ballot proposerParentBallot, Ballot ballot, object value) => manager.Accept(key, proposerParentBallot, ballot, value);

        public ValueTask<List<string>> GetKeys() => store.GetKeys(int.MaxValue);
    }
}