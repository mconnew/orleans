using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Runtime;
using Orleans.Serialization;

namespace Orleans.MetadataStore.Tests
{
    public class MetadataStoreClusteringOptions
    {
        public int MinimumNodes { get; set; }
        public SiloAddress[] SeedNodes { get; set; }

        public override string ToString() => $"[SeedNodes: [{string.Join(",", this.SeedNodes?.Select(s => s.ToString()) ?? Array.Empty<string>())}]]";
    }

    public class MetadataStoreMembershipTable : IMembershipTable, ILifecycleParticipant<ISiloLifecycle>
    {
        private const string MembershipKey = "membership";
        private readonly ILocalSiloDetails localSiloDetails;
        private readonly ConfigurationManager configurationManager;
        private readonly IOptionsMonitor<MetadataStoreClusteringOptions> options;
        private readonly SerializationManager serializationManager;
        private readonly ConfigurationUpdater<MembershipTableData> updateMembershipTable;
        private readonly CancellationTokenSource shutdownTokenSource = new CancellationTokenSource();
        private readonly ILogger<MetadataStoreMembershipTable> log;
        private readonly IServiceProvider serviceProvider;
        private Task waitForClusterStability = Task.CompletedTask;

        public MetadataStoreMembershipTable(
                ILocalSiloDetails localSiloDetails,
                ConfigurationManager configurationManager,
                IOptionsMonitor<MetadataStoreClusteringOptions> options,
                SerializationManager serializationManager,
                ILogger<MetadataStoreMembershipTable> log,
                IServiceProvider serviceProvider)
        {
            this.localSiloDetails = localSiloDetails;
            this.configurationManager = configurationManager;
            this.options = options;
            this.serializationManager = serializationManager;
            this.log = log;
            this.serviceProvider = serviceProvider;
            this.updateMembershipTable = this.UpdateMembershipTable;
        }

        private async Task MonitorSeedNodesAsync()
        {
            var semaphore = new SemaphoreSlim(1);
            using var onChangeHandler = this.options.OnChange(_ => semaphore.Release());

            var clusterMembershipService = this.serviceProvider.GetRequiredService<IClusterMembershipService>();
            var converged = false;
            var random = new Random();

            var waitSignal = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            this.waitForClusterStability = waitSignal.Task;

            while (!this.shutdownTokenSource.IsCancellationRequested)
            {
                try
                {
                    if (converged)
                    {
                        await semaphore.WaitAsync(this.shutdownTokenSource.Token);
                    }

                    var snapshot = this.options.CurrentValue;

                    // First, find out what the accepted version of the configuration is telling us.
                    var accepted = this.configurationManager.AcceptedConfiguration?.Configuration;
                    var acceptedSeedNodes = accepted?.Nodes?.ToSet() ?? new HashSet<SiloAddress>();

                    // Second, see what the configuration is telling us we should be seeing.
                    var snapshotSeedNodes = snapshot.SeedNodes ?? Array.Empty<SiloAddress>();
                    converged = acceptedSeedNodes.Count == snapshotSeedNodes.Length;
                    foreach (var node in snapshotSeedNodes)
                    {
                        if (!acceptedSeedNodes.Contains(node))
                        {
                            converged = false;
                            break;
                        }
                    }

                    // Either pause or resume membership table operations.
                    if (acceptedSeedNodes.Count < 2)
                    {
                        if (waitSignal.Task.IsCompleted)
                        {
                            this.log.LogInformation("Pausing cluster membership operations while waiting for more nodes.");
                            waitSignal = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                            this.waitForClusterStability = waitSignal.Task;
                        }
                    }
                    else if (!waitSignal.Task.IsCompleted)
                    {
                        this.log.LogInformation("Resuming cluster membership operations");
                        waitSignal.TrySetResult(0);
                    }

                    // Ensure that any differences are accounted for.
                    if (!converged)
                    {
                        this.log.LogInformation("Converging with configuration snapshot {Configuration}", snapshot);

                        if (acceptedSeedNodes.Count == 0)
                        {
                            await this.configurationManager.ForceLocalConfiguration(
                                new ReplicaSetConfiguration(
                                    stamp: Ballot.Zero,
                                    version: 0,
                                    nodes: snapshotSeedNodes,
                                    acceptQuorum: 1,
                                    prepareQuorum: 1,
                                    ranges: default,
                                    values: default));
                        }

                        // TODO: we must enforce changes to be at most one node at a time to maintain linearizability.
                        // The order should be: remove dead nodes, add new live nodes. This process should repeat until convergence.
                        var result = await this.configurationManager.TryUpdate(UpdateSeedConfiguration, snapshot);

                        if (result.Success)
                        {
                            this.log.LogInformation("Successfully converged with configuration snapshot. Replica Set Configuration: {ReplicaSetConfiguration}", result.Value);
                            converged = true;
                        }
                        else
                        {
                            this.log.LogInformation("Unable to converged with configuration snapshot. Replica Set Configuration: {ReplicaSetConfiguration}", result.Value);

                            // Wait a pseudorandom amount of time to reduce the chance of repeated races.
                            await Task.Delay(random.Next(500, 5_000));
                        }

                        // Signal a refresh but do not wait for it to complete.
                        // The refresh may be blocked by this convergence process.
                        _ = clusterMembershipService.Refresh();
                    }
                    else
                    {
                        this.log.LogInformation("Converged with configuration snapshot {Configuration}", snapshot);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception exception)
                {
                    this.log.LogError(exception, "Error while applying configuration");
                    await Task.Delay(1_000);
                }
            }

            static (bool ShouldUpdate, ReplicaSetConfigurationUpdate Update) UpdateSeedConfiguration(ReplicaSetConfiguration existing, MetadataStoreClusteringOptions snapshot)
            {
                var seedNodes = snapshot.SeedNodes ?? Array.Empty<SiloAddress>();
                var existingNodes = existing.Nodes?.ToSet() ?? new HashSet<SiloAddress>();

                var different = existingNodes.Count == seedNodes.Length;
                if (!different)
                {
                    foreach (var node in seedNodes)
                    {
                        if (!existingNodes.Contains(node))
                        {
                            different = true;
                            break;
                        }
                    }
                }

                if (!different)
                {
                    return (false, default);
                }

                return (true, new ReplicaSetConfigurationUpdate(nodes: seedNodes, ranges: existing.Ranges, values: existing.Values));
            }
        }

        public async Task CleanupDefunctSiloEntries(DateTimeOffset beforeDate)
        {
            await waitForClusterStability;
            // TODO: impl
            throw new NotImplementedException();
        }

        public async Task DeleteMembershipTableEntries(string clusterId)
        {
            await waitForClusterStability;
            // TODO: impl
            throw new NotImplementedException();
        }

        public Task InitializeMembershipTable(bool tryInitTableVersion) => this.waitForClusterStability;

        public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
        {
            await waitForClusterStability;
            var existing = await RefreshCommitted(tableVersion);
            if (existing is null || existing.Version.Version >= tableVersion.Version)
            {
                return false;
            }

            // No duplicates.
            foreach (var member in existing.Members)
            {
                if (member.Item1.SiloAddress.Equals(entry.SiloAddress))
                {
                    return false;
                }
            }

            var updatedMembers = new List<Tuple<MembershipEntry, string>>(existing.Members)
            {
                Tuple.Create(entry, tableVersion.ToString())
            };

            var updated = new MembershipTableData(updatedMembers, tableVersion);
            var result = await this.configurationManager.TryUpdate(this.updateMembershipTable, updated);
            return result.Success;
        }

        public async Task<MembershipTableData> ReadAll()
        {
            await waitForClusterStability;
            var (success, result) = await this.configurationManager.TryRead();
            if (!success)
            {
                return Zero();
            }

            return this.TryGetValue(result) ?? Zero();

            static MembershipTableData Zero() => new MembershipTableData(new List<Tuple<MembershipEntry, string>>(), new TableVersion(0, "zero"));
        }

        public Task<MembershipTableData> ReadRow(SiloAddress key) => ReadAll();

        public async Task UpdateIAmAlive(MembershipEntry entry)
        {
            await waitForClusterStability;
            var existing = await ReadAll();
            if (existing is null)
            {
                // TODO: log error - doesn't exist
                return;
            }

            var exists = false;
            var updatedMembers = new List<Tuple<MembershipEntry, string>>(existing.Members);
            for (var i = 0; i < updatedMembers.Count; i++)
            {
                var member = updatedMembers[i];
                if (member.Item1.SiloAddress.Equals(entry.SiloAddress))
                {
                    updatedMembers[i] = Tuple.Create(entry, member.Item2);
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                // TODO: log error - doesn't exist
                return;
            }

            var updated = new MembershipTableData(updatedMembers, existing.Version);
            var result = await this.configurationManager.TryUpdate(this.updateMembershipTable, updated);
            if (!result.Success)
            {
                // TODO: log error/retry - failed.
                // TODO: throw.
            }
        }

        public async Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
        {
            await waitForClusterStability;
            var existing = await RefreshCommitted(tableVersion);
            if (existing is null || existing.Version.Version >= tableVersion.Version)
            {
                return false;
            }

            var exists = false;
            var updatedMembers = new List<Tuple<MembershipEntry, string>>(existing.Members);
            for (var i = 0; i < updatedMembers.Count; i++)
            {
                var member = updatedMembers[i];
                if (member.Item1.SiloAddress.Equals(entry.SiloAddress))
                {
                    updatedMembers[i] = Tuple.Create(entry, etag);
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                return false;
            }

            var updated = new MembershipTableData(updatedMembers, tableVersion);
            var result = await this.configurationManager.TryUpdate(this.updateMembershipTable, updated);
            return result.Success;
        }

        private ValueTask<MembershipTableData> RefreshCommitted(TableVersion expectedVersion)
        {
            var result = TryGetAcceptedValue();
            if (result is object && (expectedVersion == null || result.Version.Version >= expectedVersion.Version))
            {
                return new ValueTask<MembershipTableData>(result);
            }

            return new ValueTask<MembershipTableData>(this.ReadAll());
        }

        private MembershipTableData TryGetAcceptedValue()
        {
            var accepted = this.configurationManager.AcceptedConfiguration?.Configuration;
            return TryGetValue(accepted);
        }

        private MembershipTableData TryGetValue(ReplicaSetConfiguration configuration)
        {
            string serialized = null;
            if (configuration?.Values?.TryGetValue(MembershipKey, out serialized) != true || string.IsNullOrWhiteSpace(serialized))
            {
                return null;
            }

            return this.serializationManager.DeserializeFromByteArray<MembershipTableData>(Convert.FromBase64String(serialized));
        }

        private (bool ShouldUpdate, ReplicaSetConfigurationUpdate Update) UpdateMembershipTable(ReplicaSetConfiguration existing, MembershipTableData value)
        {
            var current = TryGetValue(existing);
            if (current is null || (string.Equals(current.Version.VersionEtag, value.Version.VersionEtag) && current.Version.Version < value.Version.Version))
            {
                var serialized = Convert.ToBase64String(this.serializationManager.SerializeToByteArray(value));
                var updatedValues = existing.Values.SetItem(MembershipKey, serialized);

                var existingNodes = existing.Nodes;
                var proposedNodes = value.GetSiloStatuses(filter: s => !s.IsUnavailable(), includeMyself: true, this.localSiloDetails.SiloAddress).Keys.ToArray();
                var updatedNodes = GetUpdatedNodes(existingNodes, proposedNodes);

                return (true, new ReplicaSetConfigurationUpdate(nodes: updatedNodes, ranges: existing.Ranges, updatedValues));
            }
            else
            {
                return (false, default);
            }
        }

        private SiloAddress[] GetUpdatedNodes(SiloAddress[] existingNodes, SiloAddress[] proposedNodes)
        {
            foreach (var proposed in proposedNodes)
            {
                var exists = false;
                foreach (var existing in existingNodes)
                {
                    if (proposed.Equals(existing))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    // Add a single node.
                    return Add(existingNodes, proposed);
                }
            }

            foreach (var existing in existingNodes)
            {
                var exists = false;
                foreach (var proposed in proposedNodes)
                {
                    if (proposed.Equals(existing))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    // Remove a single node.
                    return Remove(existingNodes, existing);
                }
            }

            return existingNodes;

            static SiloAddress[] Add(SiloAddress[] existingNodes, SiloAddress nodeToAdd)
            {
                // Add the new node to the list of nodes, being sure not to add a duplicate.
                var newNodes = new SiloAddress[(existingNodes?.Length ?? 0) + 1];
                if (existingNodes != null)
                {
                    for (var i = 0; i < existingNodes.Length; i++)
                    {
                        // If the configuration already contains the specified node, return the already-confirmed configuration.
                        if (existingNodes[i].Equals(nodeToAdd))
                        {
                            return existingNodes;
                        }

                        newNodes[i] = existingNodes[i];
                    }
                }

                // Add the new node at the end.
                newNodes[newNodes.Length - 1] = nodeToAdd;
                return newNodes;
            }

            static SiloAddress[] Remove(SiloAddress[] existingNodes, SiloAddress nodeToRemove)
            {
                if (existingNodes == null || existingNodes.Length == 0) return existingNodes;

                // Remove the node from the list of nodes.
                var newNodes = new SiloAddress[existingNodes.Length - 1];
                var skipped = 0;
                for (var i = 0; i < existingNodes.Length; i++)
                {
                    var current = existingNodes[i + skipped];

                    // If the node is encountered, skip it.
                    if (current.Equals(nodeToRemove))
                    {
                        skipped = 1;
                        continue;
                    }

                    // If the array bound has been hit, then either the last element is the target
                    // or the target is not present.
                    if (i == newNodes.Length)
                    {
                        return existingNodes;
                    }

                    newNodes[i] = current;
                }

                // If no nodes changed, return a reference to the original configuration.
                if (skipped == 0) return existingNodes;

                return newNodes;
            }
        }

        public void Participate(ISiloLifecycle lifecycle)
        {
            var monitorOptionsTask = new Task[1];
            lifecycle.Subscribe(
                nameof(MetadataStoreMembershipTable),
                ServiceLifecycleStage.RuntimeInitialize,
                cancellation =>
                {
                    monitorOptionsTask[0] = Task.Run(this.MonitorSeedNodesAsync);
                    return Task.CompletedTask;
                },
                async cancellation =>
                {
                    this.shutdownTokenSource.Cancel();
                    if (monitorOptionsTask[0] is Task task) await task;
                });
        }
    }
}
