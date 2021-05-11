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
    [FixedPlacement]
    internal class RemoteMetadataStoreGrain : Grain, IRemoteMetadataStoreGrain
    {
        public const string GrainTypeString = "sys.ckv";
        public static readonly GrainType GrainType = GrainType.Create(GrainTypeString);

        private readonly MetadataStoreManager manager;
        private readonly ILocalStore store;

        public RemoteMetadataStoreGrain(
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