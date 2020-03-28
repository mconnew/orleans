using System;
using Orleans.Runtime;

namespace Orleans.Metadata
{
    public interface IGrainTypeProvider
    {
        /// <summary>
        /// Returns the grain type corresponding to the class identified by <paramref name="type"/> or <see langword="null" /> if
        /// this provider does not apply to the provided type.
        /// </summary>
        GrainType? GetGrainType(Type type);
    }

    public class AttributeGrainTypeProvider : IGrainTypeProvider
    {
        private readonly IServiceProvider serviceProvider;

        public AttributeGrainTypeProvider(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public GrainType? GetGrainType(Type grainClass)
        {
            foreach (var attr in grainClass.GetCustomAttributes(inherit: true))
            {
                if (attr is IGrainTypeProviderAttribute typeProviderAttribute)
                {
                    return typeProviderAttribute.GetGrainType(this.serviceProvider, grainClass);
                }
            }

            return default;
        }
    }

    /// <summary>
    /// An <see cref="Attribute"/> which implements this specifies the <see cref="GrainType"/> of the
    /// type which it is attached to.
    /// </summary>
    public interface IGrainTypeProviderAttribute
    {
        GrainType GetGrainType(IServiceProvider services, Type type);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public abstract class GrainTypeProviderAttribute : Attribute, IGrainTypeProviderAttribute
    {
        public abstract GrainType GetGrainType(IServiceProvider services, Type type);
    }

    /// <summary>
    /// Specifies the grain type of the type which it is attached to.
    /// </summary>
    public sealed class GrainTypeAttribute : GrainTypeProviderAttribute
    {
        private readonly GrainType grainType;

        public GrainTypeAttribute(string grainType)
        {
            this.grainType = GrainType.Create(grainType);
        }

        public override GrainType GetGrainType(IServiceProvider services, Type type) => this.grainType;
    }
}
