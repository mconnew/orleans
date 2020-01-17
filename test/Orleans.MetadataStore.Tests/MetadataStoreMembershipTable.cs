using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Runtime;
using Orleans.Serialization;

namespace Orleans.MetadataStore.Tests
{
    public class MetadataStoreClusteringOptions
    {
        public SiloAddress[] SeedNodes { get; set; }
    }

    public class MetadataStoreMembershipTable : IMembershipTable, ILifecycleParticipant<ISiloLifecycle>
    {
        private const string MembershipKey = "membership";
        private readonly ILocalSiloDetails localSiloDetails;
        private readonly ConfigurationManager configurationManager;
        private readonly IOptionsMonitor<MetadataStoreClusteringOptions> options;
        private readonly SerializationManager serializationManager;
        private readonly ConfigurationUpdater<MembershipTableData> updateFunc;

        public MetadataStoreMembershipTable(
                ILocalSiloDetails localSiloDetails,
                ConfigurationManager configurationManager,
                IOptionsMonitor<MetadataStoreClusteringOptions> options,
                SerializationManager serializationManager)
        {
            this.localSiloDetails = localSiloDetails;
            this.configurationManager = configurationManager;
            this.options = options;
            this.serializationManager = serializationManager;
            this.updateFunc = this.UpdateConfiguration;
        }

        public Task CleanupDefunctSiloEntries(DateTimeOffset beforeDate)
        {
            // TODO: impl
            throw new NotImplementedException();
        }

        public Task DeleteMembershipTableEntries(string clusterId)
        {
            // TODO: impl
            throw new NotImplementedException();
        }

        public Task InitializeMembershipTable(bool tryInitTableVersion) => Task.CompletedTask;

        public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
        {
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
            var result = await this.configurationManager.TryUpdate(this.updateFunc, updated);
            return result.Success;
        }

        public async Task<MembershipTableData> ReadAll()
        {
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
            var result = await this.configurationManager.TryUpdate(this.updateFunc, updated);
            if (!result.Success)
            {
                // TODO: log error/retry - failed.
                // TODO: throw.
            }
        }

        public async Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
        {
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
            var result = await this.configurationManager.TryUpdate(this.updateFunc, updated);
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

        private (bool ShouldUpdate, ReplicaSetConfigurationUpdate Update) UpdateConfiguration(ReplicaSetConfiguration existing, MembershipTableData value)
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
            lifecycle.Subscribe(
                nameof(MetadataStoreMembershipTable),
                ServiceLifecycleStage.RuntimeInitialize,
                async cancellation =>
                {
                    await this.configurationManager.ForceLocalConfiguration(
                        new ReplicaSetConfiguration(
                            stamp: new Ballot(1, this.configurationManager.NodeId),
                            version: 1,
                            nodes: this.options.CurrentValue.SeedNodes,
                            acceptQuorum: 1,
                            prepareQuorum: 1,
                            ranges: default,
                            values: default));
                });
        }
    }
}
