using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.MetadataStore.Storage;
using Orleans.Runtime;
using AsyncEx = Nito.AsyncEx;

namespace Orleans.MetadataStore
{
    [GenerateSerializer]
    public struct ReplicaSetConfigurationUpdate
    {
        public ReplicaSetConfigurationUpdate(SiloAddress[] nodes, RangeMap? ranges, ImmutableDictionary<string, string> values)
        {
            this.Nodes = nodes;
            this.Ranges = ranges ?? default;
            this.Values = values ?? ImmutableDictionary<string, string>.Empty;
        }

        [Id(0)]
        public SiloAddress[] Nodes { get; }

        [Id(1)]
        public RangeMap Ranges { get; }

        [Id(2)]
        public ImmutableDictionary<string, string> Values { get; }
    }

    public delegate (bool ShouldUpdate, ReplicaSetConfigurationUpdate Update) ConfigurationUpdater<T>(ReplicaSetConfiguration existingConfiguration, T input);

    /// <summary>
    /// The Configuration Manager is responsible for coordinating configuration (cluster membership) changes
    /// across the cluster.
    /// </summary>
    /// <remarks>
    /// From a high level, cluster configuration is stored in a special-purpose shared register. The linearizability
    /// properties of this register are used to ensure that safety requirements of the system are not violated.
    /// In particular, at no point in time and under no situation 
    /// </remarks>
    public class ConfigurationManager : IProposer<IVersioned>, IAcceptor<IVersioned>
    {
        public const string ClusterConfigurationKey = "MDS.Config";
        private const string NodeIdKey = "MDS.NodeId";

        private static readonly Func<Ballot> getAcceptorParentBallot = () => Ballot.Zero;
        private readonly ILocalStore store;
        private readonly IStoreReferenceFactory referenceFactory;
        private readonly IServiceProvider serviceProvider;
        private readonly MetadataStoreOptions options;

        private readonly ChangeFunction<ReplicaSetConfiguration, IVersioned> updateFunction =
            (current, updated) => (current?.Version ?? 0) == updated.Version - 1 ? updated : current;
        private readonly ConfigurationUpdater<SiloAddress> addFunction;
        private readonly ConfigurationUpdater<SiloAddress> removeFunction;
        private readonly ChangeFunction<object, IVersioned> readFunction = (current, updated) => current;
        private readonly AsyncEx.AsyncLock updateLock = new AsyncEx.AsyncLock();
        private readonly Proposer<IVersioned> proposer;
        private readonly Acceptor<IVersioned> acceptor;
        private readonly ILogger<ConfigurationManager> log;

        public ConfigurationManager(
            ILocalStore store,
            ILoggerFactory loggerFactory,
            IStoreReferenceFactory referenceFactory,
            IOptions<MetadataStoreOptions> options,
            IServiceProvider serviceProvider)
        {
            this.store = store;
            this.referenceFactory = referenceFactory;
            this.serviceProvider = serviceProvider;
            this.log = loggerFactory.CreateLogger<ConfigurationManager>();
            this.options = options.Value;
            this.addFunction = this.AddServer;
            this.removeFunction = this.RemoveServer;

            this.acceptor = new Acceptor<IVersioned>(
                key: ClusterConfigurationKey,
                store: store,
                getParentBallot: getAcceptorParentBallot,
                onUpdateState: this.OnUpdateConfiguration,
                log: loggerFactory.CreateLogger("MetadataStore.ConfigAcceptor")
            );

            // The config proposer always uses the configuration which it's proposing.
            this.proposer = new Proposer<IVersioned>(
                key: ClusterConfigurationKey,
                initialBallot: Ballot.Zero,
                getConfiguration: () => this.AcceptedConfiguration,
                log: loggerFactory.CreateLogger("MetadataStore.ConfigProposer")
            );
        }

        private ValueTask OnUpdateConfiguration(RegisterState<IVersioned> state)
        {
            //using (await this.updateLock.LockAsync())
            {
                this.AcceptedConfiguration = ExpandedReplicaSetConfiguration.Create((ReplicaSetConfiguration)state.Value, this.options, this.referenceFactory);
                //this.ProposedConfiguration = null;
            }
            return default;
        }

        /// <summary>
        /// Returns the most recently accepted configuration. Note that this configuration may not be committed.
        /// </summary>
        public ExpandedReplicaSetConfiguration AcceptedConfiguration { get; private set; }

        /// <summary>
        /// Returns the most recently proposed configuration.
        /// </summary>
        public ExpandedReplicaSetConfiguration ProposedConfiguration { get; private set; }

        /// <summary>
        /// Returns the active configuration.
        /// </summary>
        public ExpandedReplicaSetConfiguration ActiveConfiguration => this.ProposedConfiguration ?? this.AcceptedConfiguration;

        public int NodeId { get; set; }

        public async Task Initialize()
        {
            // TODO: Implement some persistence for NodeId?
            //this.NodeId = await this.store.Read<int>(NodeIdKey);
            if (this.NodeId == 0)
            {
                this.NodeId = Math.Abs(Guid.NewGuid().GetHashCode());
                //await this.store.Write(NodeIdKey, this.NodeId);
            }

            this.proposer.Ballot = new Ballot(0, this.NodeId);
            await this.acceptor.EnsureStateLoaded();
        }

        public Task ForceLocalConfiguration(ReplicaSetConfiguration configuration) => this.acceptor.ForceState(configuration);

        public Task<UpdateResult<ReplicaSetConfiguration>> TryAddServer(SiloAddress address) => this.ModifyConfiguration(this.addFunction, address);

        public Task<UpdateResult<ReplicaSetConfiguration>> TryRemoveServer(SiloAddress address) => this.ModifyConfiguration(this.removeFunction, address);

        public Task<UpdateResult<ReplicaSetConfiguration>> TryUpdate<T>(ConfigurationUpdater<T> func, T state) => this.ModifyConfiguration(func, state);

        public async Task<ReadResult<ReplicaSetConfiguration>> TryRead(CancellationToken cancellationToken = default)
        {
            var result = await this.proposer.TryUpdate(null, this.readFunction, cancellationToken);
            return new ReadResult<ReplicaSetConfiguration>(result.Item1 == ReplicationStatus.Success, result.Item2 as ReplicaSetConfiguration);
        }

        private async Task<UpdateResult<ReplicaSetConfiguration>> ModifyConfiguration<T>(ConfigurationUpdater<T> changeFunc, T input)
        {
            // Update the configuration using two consensus rounds, first reading/committing the existing configuration,
            // then modifying it to add or remove a single server and committing the new value.
            // 
            // Note that performing the update using a single consensus round could break the invariant that configuration
            // grows or shrinks by at most one node at a time. For example, consider a scenario in which a commit was only
            // accepted on one acceptor in a set before the proposer faulted. In that case, the configuration may be seen
            // by the hypothetical single read-modify-write consensus round before the majority of acceptors are using
            // that configuration. The effect is that the majority may see a configuration change which changes by two
            // or more nodes simultaneously.
            var cancellation = CancellationToken.None;
            using (await this.updateLock.LockAsync())
            {
                // Read the currently committed configuration, potentially committing a partially-committed configuration in the process.
                var (status, committedValue) = await this.proposer.TryUpdate(null, this.readFunction, cancellation);
                var committedConfig = (ReplicaSetConfiguration) committedValue;
                if (status != ReplicationStatus.Success)
                {
                    return new UpdateResult<ReplicaSetConfiguration>(false, committedConfig);
                }

                // Modify the replica set.
                var (shouldUpdate, update) = changeFunc(committedConfig, input);
                if (!shouldUpdate)
                {
                    // The new address was already in the committed configuration, so no additional work needs to be done.
                    return new UpdateResult<ReplicaSetConfiguration>(false, committedConfig);
                }

                // Assemble the new configuration.
                var committedStamp = committedConfig?.Stamp ?? default(Ballot);
                this.proposer.Ballot = this.proposer.Ballot.AdvanceTo(committedStamp);
                var newStamp = this.proposer.Ballot.Successor();

                var quorum = update.Nodes.Length / 2 + 1;
                var updatedConfig = new ReplicaSetConfiguration(
                    stamp: newStamp,
                    version: (committedValue?.Version ?? 0) + 1,
                    nodes: update.Nodes,
                    acceptQuorum: quorum,
                    prepareQuorum: quorum,
                    ranges: update.Ranges,
                    values: update.Values);
                var success = false;

                try
                {
                    this.ProposedConfiguration = ExpandedReplicaSetConfiguration.Create(updatedConfig, this.options, this.referenceFactory);

                    // Attempt to commit the new configuration.
                    (status, committedValue) = await this.proposer.TryUpdate(updatedConfig, this.updateFunction, cancellation);
                    success = status == ReplicationStatus.Success;

                    // Ensure that a quorum of acceptors have the latest value for all keys.
                    //if (success) success = await this.CatchupAllAcceptors();

                    return new UpdateResult<ReplicaSetConfiguration>(success, (ReplicaSetConfiguration)committedValue);
                }
                finally
                {
                    if (success) this.AcceptedConfiguration = this.ProposedConfiguration;
                }
            }
        }

        private async Task<bool> CatchupAllAcceptors()
        {
            var (success, allKeys) = await GetAllKeys();
            if (success)
            {
                this.log.LogError($"Failed to successfully read keys from a quorum of nodes.");
                return false;
            }

            var storeManager = this.serviceProvider.GetRequiredService<IMetadataStore>();
            var batchTasks = new List<Task>(100);
            foreach (var batch in allKeys.BatchIEnumerable(100))
            {
                foreach (var key in batch)
                {
                    batchTasks.Add(storeManager.TryGet<IVersioned>(key));
                }

                await Task.WhenAll(batchTasks);
            }

            return true;
        }

        private async Task<(bool, HashSet<string>)> GetAllKeys()
        {
            var quorum = this.ActiveConfiguration.Configuration.PrepareQuorum;
            var remainingConfirmations = quorum;
            var storeReferences = this.ActiveConfiguration.StoreReferences;
            var allKeys = new HashSet<string>();
            foreach (var storeReference in storeReferences)
            {
                var remoteMetadataStore = storeReference[0];

                try
                {
                    var storeKeys = await remoteMetadataStore.GetKeys();

                    foreach (var key in storeKeys)
                    {
                        allKeys.Add(key);
                    }

                    --remainingConfirmations;

                    if (remainingConfirmations == 0) break;
                }
                catch (Exception exception)
                {
                    this.log.LogWarning($"Exception calling {nameof(IRemoteMetadataStore.GetKeys)} on remote store {remoteMetadataStore}: {exception}");
                }
            }

            var success = remainingConfirmations > 0;
            return (success, allKeys);
        }

        private (bool ShouldUpdate, ReplicaSetConfigurationUpdate Update) AddServer(ReplicaSetConfiguration existingConfiguration, SiloAddress nodeToAdd)
        {
            var existingNodes = existingConfiguration?.Nodes;

            // Add the new node to the list of nodes, being sure not to add a duplicate.
            var newNodes = new SiloAddress[(existingNodes?.Length ?? 0) + 1];
            if (existingNodes != null)
            {
                for (var i = 0; i < existingNodes.Length; i++)
                {
                    // If the configuration already contains the specified node, return the already-confirmed configuration.
                    if (existingNodes[i].Equals(nodeToAdd))
                    {
                        return (false, default);
                    }

                    newNodes[i] = existingNodes[i];
                }
            }

            // Add the new node at the end.
            newNodes[newNodes.Length - 1] = nodeToAdd;
            return (true, new ReplicaSetConfigurationUpdate(newNodes, existingConfiguration?.Ranges, existingConfiguration?.Values));
        }

        private (bool ShouldUpdate, ReplicaSetConfigurationUpdate Update) RemoveServer(ReplicaSetConfiguration existingConfiguration, SiloAddress nodeToRemove)
        {
            var existingNodes = existingConfiguration?.Nodes;
            if (existingNodes == null || existingNodes.Length == 0) return (false, default);

            // Remove the node from the list of nodes.
            var newNodes = new SiloAddress[existingNodes.Length - 1];
            var removed = false;
            for (var i = 0; i < existingNodes.Length; i++)
            {
                var current = existingNodes[i];

                // If the node is encountered, skip it.
                if (current.Equals(nodeToRemove))
                {
                    removed = true;
                    continue;
                }

                newNodes[i - (removed ? 1 : 0)] = current;
            }

            // If no nodes changed, return a reference to the original configuration.
            if (!removed) return (false, default);

            return (true, new ReplicaSetConfigurationUpdate(newNodes, existingConfiguration?.Ranges, existingConfiguration?.Values));
        }

        Task<(ReplicationStatus, IVersioned)> IProposer<IVersioned>.TryUpdate<TArg>(
            TArg value,
            ChangeFunction<TArg, IVersioned> changeFunction,
            CancellationToken cancellationToken) =>
            this.proposer.TryUpdate(value, changeFunction, cancellationToken);

        ValueTask<PrepareResponse> IAcceptor<IVersioned>.Prepare(Ballot proposerParentBallot, Ballot ballot) =>
            this.acceptor.Prepare(proposerParentBallot, ballot);

        ValueTask<AcceptResponse> IAcceptor<IVersioned>.Accept(Ballot proposerParentBallot, Ballot ballot, IVersioned value) =>
            this.acceptor.Accept(proposerParentBallot, ballot, value);
    }
}
