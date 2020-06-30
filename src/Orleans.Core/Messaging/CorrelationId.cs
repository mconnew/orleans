using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Orleans.Runtime
{
    [Serializable]
    internal readonly struct CorrelationId : IEquatable<CorrelationId>, IComparable<CorrelationId>
    {
        private static long nextToUse = 1;
        private readonly long id;

        public CorrelationId(long value)
        {
            id = value;
        }

        public long Value => id;

        public static CorrelationId GetNext()
        {
            var val = Interlocked.Increment(ref nextToUse);
            return new CorrelationId(val);
        }

        public override int GetHashCode() => id.GetHashCode();

        public override bool Equals(object obj)
        {
            if (!(obj is CorrelationId id))
            {
                return false;
            }

            return this.Equals(id);
        }

        public bool Equals(CorrelationId other)
        {
            return !ReferenceEquals(other, null) && (id == other.id);
        }

        public static bool operator ==(CorrelationId lhs, CorrelationId rhs) => (rhs.id == lhs.id);

        public static bool operator !=(CorrelationId lhs, CorrelationId rhs) => rhs.id != lhs.id;

        public int CompareTo(CorrelationId other) => id.CompareTo(other.id);

        public override string ToString() => id.ToString();

        public sealed class Comparer : IEqualityComparer<CorrelationId>
        {
            public static Comparer Instance { get; } = new Comparer();

            public bool Equals(CorrelationId x, CorrelationId y) => x.Equals(y);

            public int GetHashCode(CorrelationId obj) => obj.GetHashCode();
        }
    }
}
