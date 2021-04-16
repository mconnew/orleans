using System;

namespace Orleans.Serialization
{
    /// <summary>
    /// Values for identifying <see cref="IKeyedSerializer"/> serializers.
    /// </summary>
    internal enum KeyedSerializerId : byte
    {
        [Obsolete("Removed")]
        ILBasedSerializer = 1,

        [Obsolete("Removed")]
        BinaryFormatterISerializable = 2,

        [Obsolete("Removed")]
        ISerializableSerializer = 3,

        /// <summary>
        /// The maximum reserved value.
        /// </summary>
        MaxReservedValue = 100,
    }
}