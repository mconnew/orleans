using System;
using System.Buffers.Binary;
using System.Runtime.Serialization;

namespace Orleans.Runtime
{
    [Serializable, Immutable]
    [GenerateSerializer]
    public readonly struct ActivationId : IEquatable<ActivationId>
    {
        [DataMember(Order = 0)]
        [Id(0)]
        internal readonly Guid Key;

        public bool IsDefault => Equals(Zero);

        public static readonly ActivationId Zero = GetActivationId(Guid.Empty);

        private ActivationId(Guid key) => Key = key;

        public static ActivationId NewId() => GetActivationId(Guid.NewGuid());

        public static ActivationId GetDeterministic(GrainId grain)
        {
#if !NETCOREAPP3_1_OR_GREATER
            byte[] temp = new byte[16];
#else
            Span<byte> temp = stackalloc byte[16];
#endif
            var a = (ulong)grain.Type.GetUniformHashCode();
            var b = (ulong)grain.Key.GetUniformHashCode();
            BinaryPrimitives.WriteUInt64LittleEndian(temp, a);
#if !NETCOREAPP3_1_OR_GREATER
            BinaryPrimitives.WriteUInt64LittleEndian(temp.AsSpan(8), b);
#else
            BinaryPrimitives.WriteUInt64LittleEndian(temp[8..], b);
#endif
            var key = new Guid(temp);
            return new ActivationId(key);
        }

        internal static ActivationId GetActivationId(Guid key) => new(key);

        public override bool Equals(object obj) => obj is ActivationId other && Key.Equals(other.Key);

        public bool Equals(ActivationId other) => Key.Equals(other.Key);

        public override int GetHashCode() => Key.GetHashCode();

        public override string ToString() => $"@{Key:N}";

        public string ToFullString() => ToString();

        public static bool operator ==(ActivationId left, ActivationId right) => left.Equals(right);

        public static bool operator !=(ActivationId left, ActivationId right) => !(left == right);
    }
}
