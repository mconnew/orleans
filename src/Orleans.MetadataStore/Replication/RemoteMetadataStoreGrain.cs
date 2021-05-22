using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Orleans.MetadataStore.Storage;
using Orleans.Placement;
using Orleans.Runtime;

namespace Orleans.MetadataStore
{
    [Reentrant]
    [GrainType(GrainTypeString)]
    [SiloServicePlacement]
    internal class RemoteMetadataStoreGrain : Grain, IRemoteMetadataStoreGrain
    {
        public const string GrainTypeString = "sys.ckv";
        public static readonly GrainType GrainType = GrainType.Create(GrainTypeString);

        private readonly MetadataStoreManager _manager;
        private readonly ILocalStore _store;

        public RemoteMetadataStoreGrain(
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