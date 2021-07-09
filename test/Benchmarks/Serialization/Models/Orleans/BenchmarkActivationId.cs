using System;
using System.Buffers.Binary;
using System.Runtime.Serialization;

namespace FakeFx.Runtime
{
    [Serializable]
    [Orleans.GenerateSerializer]
    [Orleans.SuppressReferenceTracking]
    public readonly struct BenchmarkActivationId : IEquatable<BenchmarkActivationId>
    {
        [DataMember]
        [Orleans.Id(1)]
        internal readonly Guid Key;

        public bool IsDefault => Equals(Zero);

        public static readonly BenchmarkActivationId Zero = GetActivationId(Guid.Empty);

        private BenchmarkActivationId(Guid key) => Key = key;

        public static BenchmarkActivationId NewId() => GetActivationId(Guid.NewGuid());

        public static BenchmarkActivationId GetDeterministic(GrainId grain)
        {
            Span<byte> temp = stackalloc byte[16];
            var a = (ulong)grain.Type.GetHashCode();
            var b = (ulong)grain.Key.GetHashCode();
            BinaryPrimitives.WriteUInt64LittleEndian(temp, a);
            BinaryPrimitives.WriteUInt64LittleEndian(temp[8..], b);
            var key = new Guid(temp);
            return new BenchmarkActivationId(key);
        }

        internal static BenchmarkActivationId GetActivationId(Guid key) => new(key);

        public override bool Equals(object obj) => obj is BenchmarkActivationId other && Key.Equals(other.Key);

        public bool Equals(BenchmarkActivationId other) => Key.Equals(other.Key);

        public override int GetHashCode() => Key.GetHashCode();

        public override string ToString() => $"@{Key:N}";

        public string ToFullString() => ToString();

        public static bool operator ==(BenchmarkActivationId left, BenchmarkActivationId right) => left.Equals(right);

        public static bool operator !=(BenchmarkActivationId left, BenchmarkActivationId right) => !(left == right);
    }
}
