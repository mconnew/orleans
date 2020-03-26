using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using Orleans.Concurrency;

namespace Orleans.Runtime
{
    /// <summary>
    /// Uniquely identifies an entity.
    /// </summary>
    [Immutable]
    [Serializable]
    [StructLayout(LayoutKind.Auto)]
    public readonly struct GrainId : IEquatable<GrainId>, IComparable<GrainId>, ISerializable
    {
        private readonly GrainType _type;
        private readonly IdSpan _key;

        public GrainId(GrainType type, IdSpan key)
        {
            _type = type;
            _key = key;
        }

        public GrainId(byte[] type, byte[] key) : this(new GrainType(type), new IdSpan(key))
        {
        }

        public GrainId(GrainType type, byte[] key, int keyHashCode) : this(type, new IdSpan(key, keyHashCode)) { }


        public GrainId(GrainType type, byte[] key) : this(type, new IdSpan(key))
        {
        }

        public GrainId(SerializationInfo info, StreamingContext context)
        {
            _type = new GrainType((byte[])info.GetValue("tv", typeof(byte[])), info.GetInt32("th"));
            _key = new IdSpan((byte[])info.GetValue("kv", typeof(byte[])), info.GetInt32("kh"));
        }

        public static GrainId Create(string type, string key) => Create(GrainType.Create(type), key);

        public static GrainId Create(string type, Guid key) => Create(GrainType.Create(type), key.ToString("N"));

        public static GrainId Create(GrainType type, string key) => new GrainId(type, Encoding.UTF8.GetBytes(key));

        public static GrainId Create(GrainType type, IdSpan key) => new GrainId(type, key);

        public readonly GrainType Type => _type;

        public readonly IdSpan Key => _key;

        public readonly bool IsDefault => _type.IsDefault && _key.IsDefault;

        public override readonly bool Equals(object obj) => obj is GrainId id && this.Equals(id);

        public readonly bool Equals(GrainId other) => this.Type.Equals(other.Type) && this._key.Equals(other._key);

        public string ToParsableString()
        {
            var type = GrainType.UnsafeGetArray(this.Type);
            var key = SpanId.UnsafeGetArray(this.Key);

            // TODO: pick a better format for this and implement efficiently.
            // 1 is the version number.
            return $"1{Convert.ToBase64String(type)}*{Convert.ToBase64String(key)}";
        }

        public static GrainId FromParsableString(string value)
        {
            var version = value[0];
            if (value.IndexOf('*') is int index && index < 0)
            {
                // invalid.
            }

            var type = Convert.FromBase64String(value.Substring(1, index - 1));
            var key = Convert.FromBase64String(value.Substring(index + 1));
            return new GrainId(type, key);
        }

        public override readonly int GetHashCode() => HashCode.Combine(_type, _key);

        public readonly uint GetUniformHashCode() => unchecked((uint)this.GetHashCode());

        public readonly uint GetHashCode_Modulo(uint umod)
        {
            int key = GetHashCode();
            int mod = (int)umod;
            key = ((key % mod) + mod) % mod; // key should be positive now. So assert with checked.
            return checked((uint)key);
        }

        public readonly void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("tv", GrainType.UnsafeGetArray(_type));
            info.AddValue("th", _type.GetHashCode());
            info.AddValue("kv", IdSpan.UnsafeGetArray(_key));
            info.AddValue("kh", _key.GetHashCode());
        }

        public readonly int CompareTo(GrainId other)
        {
            var typeComparison = _type.CompareTo(other._type);
            if (typeComparison != 0) return typeComparison;

            return _key.CompareTo(other._key);
        }

        public static bool operator ==(GrainId a, GrainId b) => a.Equals(b);

        public static bool operator !=(GrainId a, GrainId b) => !a.Equals(b);

        public static bool operator >(GrainId a, GrainId b) => a.CompareTo(b) > 0;

        public static bool operator <(GrainId a, GrainId b) => a.CompareTo(b) < 0;

        public override readonly string ToString() => $"{_type.ToStringUtf8()}/{_key.ToStringUtf8()}";

        public static (byte[] Key, int KeyHashCode) UnsafeGetKey(GrainId id) => (IdSpan.UnsafeGetArray(id._key), id._key.GetHashCode());

        public static IdSpan KeyAsSpanId(GrainId id) => id._key;

        public sealed class Comparer : IEqualityComparer<GrainId>, IComparer<GrainId>
        {
            public static Comparer Instance { get; } = new Comparer();

            public int Compare(GrainId x, GrainId y) => x.CompareTo(y);

            public bool Equals(GrainId x, GrainId y) => x.Equals(y);

            public int GetHashCode(GrainId obj) => obj.GetHashCode();
        }
    }
}
