using System;
using System.Collections.Generic;
using System.Linq;
using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.Transactions.Abstractions;

namespace Orleans.Transactions
{
    [Serializable]
    [Immutable]
    public readonly struct ParticipantId
    {
        public static readonly IEqualityComparer<ParticipantId> Comparer = new IdComparer();

        [Flags]
        public enum Role
        {
            Resource = 1 << 0,
            Manager = 1 << 1,
            PriorityManager = 1 << 2
        }

        public string Name { get; }
        public GrainId GrainId { get; }
        public Role SupportedRoles { get; }

        public ParticipantId(string name, GrainId grainId, Role supportedRoles)
        {
            this.Name = name;
            this.GrainId = grainId;
            this.SupportedRoles = supportedRoles;
        }

        public override string ToString()
        {
            return $"ParticipantId.{Name}.{GrainId}";
        }

        private class IdComparer : IEqualityComparer<ParticipantId>
        {
            public bool Equals(ParticipantId x, ParticipantId y)
            {
                return string.CompareOrdinal(x.Name, y.Name) == 0 && Equals(x.GrainId, y.GrainId);
            }

            public int GetHashCode(ParticipantId obj)
            {
                unchecked
                {
                    var idHashCode = (obj.Name != null) ? obj.Name.GetHashCode() : 0;
                    var referenceHashCode = (obj.GrainId != null) ? obj.GrainId.GetHashCode() : 0;
                    return (idHashCode * 397) ^ (referenceHashCode);
                }
            }
        }
    }

    public static class ParticipantRoleExtensions
    {
        public static bool SupportsRoles(this ParticipantId participant, ParticipantId.Role role)
        {
            return (participant.SupportedRoles & role) != 0;
        }

        public static bool IsResource(this ParticipantId participant)
        {
            return participant.SupportsRoles(ParticipantId.Role.Resource);
        }

        public static bool IsManager(this ParticipantId participant)
        {
            return participant.SupportsRoles(ParticipantId.Role.Manager);
        }

        public static bool IsPriorityManager(this ParticipantId participant)
        {
            return participant.SupportsRoles(ParticipantId.Role.PriorityManager);
        }

        public static IEnumerable<KeyValuePair<ParticipantId,AccessCounter>> SelectResources(this IEnumerable<KeyValuePair<ParticipantId, AccessCounter>> participants)
        {
            return participants.Where(p => p.Key.IsResource());
        }

        public static IEnumerable<KeyValuePair<ParticipantId, AccessCounter>> SelectManagers(this IEnumerable<KeyValuePair<ParticipantId, AccessCounter>> participants)
        {
            return participants.Where(p => p.Key.IsManager());
        }

        public static IEnumerable<ParticipantId> SelectPriorityManagers(this IEnumerable<ParticipantId> participants)
        {
            return participants.Where(p => p.IsPriorityManager());
        }
    }
}
