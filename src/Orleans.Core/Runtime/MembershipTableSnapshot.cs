using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Orleans.Runtime
{
    internal sealed class MembershipTableSnapshot
    {
        public MembershipTableSnapshot(
            MembershipVersion version,
            ImmutableDictionary<SiloAddress, MembershipEntry> entries)
        {
            this.Version = version;
            this.Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        public static MembershipTableSnapshot Create(MembershipTableData table)
        {
            if (table is null) throw new ArgumentNullException(nameof(table));

            var version = new MembershipVersion(table.Version.Version);
            return Create(version, table.Members.Select(m => m.Item1));
        }

        private static MembershipTableSnapshot Create(MembershipVersion version, IEnumerable<MembershipEntry> allEntries)
        {
            var entries = ImmutableDictionary.CreateBuilder<SiloAddress, MembershipEntry>();

            if (allEntries != null)
            {
                foreach (var entry in allEntries)
                {
                    entries[entry.SiloAddress] = entry;
                }
            }

            return new MembershipTableSnapshot(version, entries.ToImmutable());
        }

        public MembershipVersion Version { get; }
        public ImmutableDictionary<SiloAddress, MembershipEntry> Entries { get; }

        public SiloStatus GetSiloStatus(SiloAddress silo)
        {
            var status = this.Entries.TryGetValue(silo, out var entry) ? entry.Status : SiloStatus.None;
            if (status == SiloStatus.None)
            {
                foreach (var member in this.Entries)
                {
                    if (member.Key.IsSuccessorOf(silo))
                    {
                        status = SiloStatus.Dead;
                        break;
                    }
                }
            }

            return status;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"[Version: {this.Version}, {this.Entries.Count} silos");
            foreach (var entry in this.Entries) sb.Append($", {entry.Value}");
            sb.Append(']');
            return sb.ToString();
        }
    }
}
