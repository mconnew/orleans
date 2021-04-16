using System;
using System.Collections.Immutable;

namespace Orleans.Runtime
{
    [Serializable]
    [Orleans.GenerateSerializer]
    public sealed class ClusterMembershipUpdate
    {
        public ClusterMembershipUpdate(ClusterMembershipSnapshot snapshot, ImmutableArray<ClusterMember> changes)
        {
            this.Snapshot = snapshot;
            this.Changes = changes;
        }

        public bool HasChanges => !this.Changes.IsDefaultOrEmpty;

        [Orleans.Id(1)]
        public ImmutableArray<ClusterMember> Changes { get; }
        [Orleans.Id(2)]
        public ClusterMembershipSnapshot Snapshot { get; }
    }
}
