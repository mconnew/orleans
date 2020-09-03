using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Threading.Tasks;

namespace Orleans.Runtime.MembershipService.InCluster
{
    internal interface IGossipContainer
    {
        List<MembershipGossip> Gossip { get; }
    }

    public enum PingStatus
    {
        Nack,
        Ack
    }

    [Serializable]
    internal class PingRequest : IGossipContainer
    {
        public List<MembershipGossip> Gossip { get; set; }
    }

    [Serializable]
    internal class PingResponse : IGossipContainer
    {
        public List<MembershipGossip> Gossip { get; set; }
    }

    [Serializable]
    internal class MembershipGossip
    {
    }

    internal interface ISwimMemberSystemTarget : ISystemTarget
    {
        ValueTask<PingResponse> Ping(PingRequest request);
        ValueTask<PingResponse> PingOther(SiloAddress target, PingRequest request);
    }

    /// <summary>
    /// Provides initial contact endpoints for seeding cluster membership.
    /// </summary>
    public interface IMembershipSeedProvider
    {
        ValueTask<List<EndPoint>> GetMembers();
    }

    public interface IWeakMembershipService
    {
        WeakMembershipSnapshot Curent { get; }
        IAsyncEnumerable<WeakMembershipSnapshot> Snapshots { get; }
    }

    public class WeakMembershipSnapshot
    {
        public WeakMembershipSnapshot(ImmutableDictionary<SiloAddress, ClusterMember> members, WeakMembershipVersion version)
        {
            this.Members = members;
            this.Version = version;
        }

        public WeakMembershipVersion Version { get; }

        public ImmutableDictionary<SiloAddress, ClusterMember> Members { get; }
    }

    [Serializable]
    public struct WeakMembershipVersion : IComparable<WeakMembershipVersion>, IEquatable<WeakMembershipVersion>
    {
        public WeakMembershipVersion(long version)
        {
            this.Value = version;
        }

        public long Value { get; private set; }

        public static WeakMembershipVersion MinValue => new WeakMembershipVersion(long.MinValue);

        public int CompareTo(WeakMembershipVersion other) => this.Value.CompareTo(other.Value);

        public bool Equals(WeakMembershipVersion other) => this.Value == other.Value;

        public override bool Equals(object obj) => obj is WeakMembershipVersion other && this.Equals(other);

        public override int GetHashCode() => this.Value.GetHashCode();

        public override string ToString() => this.Value.ToString();

        public static bool operator ==(WeakMembershipVersion left, WeakMembershipVersion right) => left.Value == right.Value;
        public static bool operator !=(WeakMembershipVersion left, WeakMembershipVersion right) => left.Value != right.Value;
        public static bool operator >=(WeakMembershipVersion left, WeakMembershipVersion right) => left.Value >= right.Value;
        public static bool operator <=(WeakMembershipVersion left, WeakMembershipVersion right) => left.Value <= right.Value;
        public static bool operator >(WeakMembershipVersion left, WeakMembershipVersion right) => left.Value > right.Value;
        public static bool operator <(WeakMembershipVersion left, WeakMembershipVersion right) => left.Value < right.Value;
    }
}
