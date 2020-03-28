using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Orleans.Metadata
{
    [Serializable]
    [DebuggerDisplay("{Value}")]
    public readonly struct GrainInterfaceId : IEquatable<GrainInterfaceId>
    {
        public readonly string Value;

        public GrainInterfaceId(string value) => this.Value = value;

        public static GrainInterfaceId Create(string value) => new GrainInterfaceId(value);

        public override bool Equals(object obj) => obj is GrainInterfaceId id && this.Equals(id);

        public bool Equals(GrainInterfaceId other) => string.Equals(this.Value, other.Value, StringComparison.Ordinal);

        public override int GetHashCode() => HashCode.Combine(this.Value);

        public override string ToString() => this.Value;
    }

    public interface IGrainInterfaceIdProvider
    {
        /// <summary>
        /// Returns the <see cref="GrainInterfaceId"/> corresponding to the class identified by <paramref name="type"/> or <see langword="null" /> if
        /// this provider does not apply to the provided type.
        /// </summary>
        GrainInterfaceId? GetGrainInterfaceId(Type type);
    }

    public class AttributeGrainInterfaceIdProvider : IGrainInterfaceIdProvider
    {
        private readonly IServiceProvider serviceProvider;

        public AttributeGrainInterfaceIdProvider(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public GrainInterfaceId? GetGrainInterfaceId(Type grainClass)
        {
            foreach (var attr in grainClass.GetCustomAttributes(inherit: true))
            {
                if (attr is IGrainInterfaceIdProviderAttribute typeProviderAttribute)
                {
                    return typeProviderAttribute.GetGrainInterfaceId(this.serviceProvider, grainClass);
                }
            }

            return default;
        }
    }

    /// <summary>
    /// An <see cref="Attribute"/> which implements this specifies the <see cref="GrainInterfaceId"/> of the
    /// type which it is attached to.
    /// </summary>
    public interface IGrainInterfaceIdProviderAttribute
    {
        GrainInterfaceId GetGrainInterfaceId(IServiceProvider services, Type type);
    }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public abstract class GrainInterfaceIdProviderAttribute : Attribute, IGrainInterfaceIdProviderAttribute
    {
        public abstract GrainInterfaceId GetGrainInterfaceId(IServiceProvider services, Type type);
    }

    /// <summary>
    /// Specifies the <see cref="GrainInterfaceId"/> of the type which it is attached to.
    /// </summary>
    public sealed class GrainInterfaceIdAttribute : GrainInterfaceIdProviderAttribute
    {
        private readonly GrainInterfaceId value;

        public GrainInterfaceIdAttribute(string value)
        {
            this.value = GrainInterfaceId.Create(value);
        }

        public override GrainInterfaceId GetGrainInterfaceId(IServiceProvider services, Type type) => this.value;
    }
}
