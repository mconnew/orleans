using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.MetadataStore.Storage;

namespace Orleans.MetadataStore
{
    internal class MetadataStoreManager : IMetadataStore
    {
        private readonly ConcurrentDictionary<string, IAcceptor<IVersioned>> _acceptors = new();
        private readonly ConcurrentDictionary<string, IProposer<IVersioned>> _proposers = new();
        private readonly ChangeFunction<IVersioned, IVersioned> _updateFunction = (current, updated) => (current?.Version ?? 0) == updated.Version - 1 ? updated : current;
        private readonly ChangeFunction<IVersioned, IVersioned> _readFunction = (current, updated) => current;
        private readonly Func<string, IProposer<IVersioned>> _proposerFactory;
        private readonly Func<string, IAcceptor<IVersioned>> _acceptorFactory;
        private readonly ConfigurationManager _configurationManager;

        public MetadataStoreManager(
            ConfigurationManager configurationManager,
            ILoggerFactory loggerFactory,
            ILocalStore localStore)
        {
            _configurationManager = configurationManager;

            // The configuration acceptor's stored configuration is used by itself as well as the storage proposers and acceptors.
            // Note that this configuration only becomes valid during startup, once the configuration acceptor is initialized.
            // The configuration proposer (initialized during startup) uses the configuration which is being proposed.
            ExpandedReplicaSetConfiguration GetAcceptedConfig() => _configurationManager.AcceptedConfiguration;
            _proposerFactory = key => new Proposer<IVersioned>(
                key,
                new Ballot(0, _configurationManager.NodeId),
                GetAcceptedConfig,
                loggerFactory.CreateLogger($"MetadataStore.Proposer[{key}]"));

            Ballot GetAcceptedConfigBallot() => _configurationManager.AcceptedConfiguration?.Configuration?.Stamp ?? Ballot.Zero;
            _acceptorFactory = key => new Acceptor<IVersioned>(
                key,
                localStore,
                GetAcceptedConfigBallot,
                onUpdateState: null,
                log: loggerFactory.CreateLogger($"MetadataStore.Acceptor[{key}]"));

            // The cluster configuration is stored in a well-known key.
            _acceptors[ConfigurationManager.ClusterConfigurationKey] = _configurationManager;
            _proposers[ConfigurationManager.ClusterConfigurationKey] = _configurationManager;
        }
        
        public async Task<ReadResult<TValue>> TryGet<TValue>(string key) where TValue : class, IVersioned
        {
            var proposer = _proposers.GetOrAdd(key, _proposerFactory);
            var (status, value) = await proposer.TryUpdate(default(IVersioned), _readFunction, CancellationToken.None);
            return new ReadResult<TValue>(status == ReplicationStatus.Success, (TValue)value);
        }

        public async Task<UpdateResult<TValue>> TryUpdate<TValue>(string key, TValue updated) where TValue : class, IVersioned
        {
            var proposer = _proposers.GetOrAdd(key, _proposerFactory);
            var (status, value) = await proposer.TryUpdate(updated, _updateFunction, CancellationToken.None);
            return new UpdateResult<TValue>(status == ReplicationStatus.Success, (TValue)value);
        }

        public ValueTask<PrepareResponse> Prepare(string key, Ballot proposerParentBallot, Ballot ballot)
        {
            var acceptor = _acceptors.GetOrAdd(key, _acceptorFactory);
            return acceptor.Prepare(proposerParentBallot, ballot);
        }

        public ValueTask<AcceptResponse> Accept(string key, Ballot proposerParentBallot, Ballot ballot, object value)
        {
            var acceptor = _acceptors.GetOrAdd(key, _acceptorFactory);
            return acceptor.Accept(proposerParentBallot, ballot, (IVersioned)value);
        }
    }
}