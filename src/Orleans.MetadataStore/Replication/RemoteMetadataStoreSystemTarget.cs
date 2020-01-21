using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.MetadataStore.Storage;
using Orleans.Runtime;

namespace Orleans.MetadataStore
{
    internal class RemoteMetadataStoreSystemTarget : SystemTarget, IRemoteMetadataStore
    {
        private readonly MetadataStoreManager manager;
        private readonly ILocalStore store;

        public RemoteMetadataStoreSystemTarget(
            GrainId grainId,
            ISiloRuntimeClient runtimeClient,
            ILocalSiloDetails siloDetails,
            ILoggerFactory loggerFactory,
            MetadataStoreManager manager,
            ILocalStore store)
            : base(grainId, siloDetails.SiloAddress, loggerFactory)
        {
            this.RuntimeClient = runtimeClient;
            this.manager = manager;
            this.store = store;
        }

        public ValueTask<PrepareResponse> Prepare(string key, Ballot proposerParentBallot, Ballot ballot) => this.manager.Prepare(key, proposerParentBallot, ballot);

        public ValueTask<AcceptResponse> Accept(string key, Ballot proposerParentBallot, Ballot ballot, object value) => this.manager.Accept(key, proposerParentBallot, ballot, value);

        public ValueTask<List<string>> GetKeys() => this.store.GetKeys(int.MaxValue);

        public ValueTask<(Ballot, IVersioned)> GetAcceptedValue(string key) => this.manager.GetAcceptedValue(key);
    }
}