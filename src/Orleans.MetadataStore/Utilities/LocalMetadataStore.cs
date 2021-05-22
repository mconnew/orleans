using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Orleans.MetadataStore.Storage;

namespace Orleans.MetadataStore
{
    internal class LocalMetadataStore : IRemoteMetadataStore
    {
        private readonly MetadataStoreManager _manager;
        private readonly ILocalStore _store;

        public LocalMetadataStore(
            MetadataStoreManager manager,
            ILocalStore store)
        {
            _manager = manager;
            _store = store;
        }

        public ValueTask<PrepareResponse<TValue>> Prepare<TValue>(string key, Ballot proposerParentBallot, Ballot ballot) => _manager.Prepare<TValue>(key, proposerParentBallot, ballot);

        public ValueTask<AcceptResponse> Accept<TValue>(string key, Ballot proposerParentBallot, Ballot ballot, TValue value) => _manager.Accept(key, proposerParentBallot, ballot, value);

        public ValueTask<List<string>> GetKeys() => _store.GetKeys(int.MaxValue);
    }
}