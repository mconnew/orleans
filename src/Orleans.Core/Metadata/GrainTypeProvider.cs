using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Orleans.Runtime;

namespace Orleans.Metadata
{
    /// <summary>
    /// Associates a <see cref="GrainType"/> with a <see cref="Type" />.
    /// </summary>
    public class GrainTypeProvider
    {
        private const string GrainSuffix = "grain";
        private readonly IGrainTypeProvider[] providers;

        public GrainTypeProvider(IEnumerable<IGrainTypeProvider> providers)
        {
            this.providers = providers.ToArray();
        }

        /// <summary>
        /// Returns the grain type for the provided class.
        /// </summary>
        /// <param name="type">The grain class.</param>
        /// <returns>The grain type for the provided class.</returns>
        public GrainType GetGrainType(Type type)
        {
            if (!type.IsClass)
            {
                throw new ArgumentException($"Argument {nameof(type)} must be a class. Provided value, \"{type}\", is not a class.", nameof(type));
            }

            // Configured providers take precedence
            foreach (var provider in this.providers)
            {
                if (provider.GetGrainType(type) is GrainType grainType)
                {
                    return grainType;
                }
            }

            // Conventions are used as a fallback
            return GetGrainTypeByConvention(type);
        }

        public static GrainType GetGrainTypeByConvention(Type type)
        {
            var name = type.Name.ToLowerInvariant();

            // Trim generic arity
            var index = name.IndexOf('`');
            if (index > 0)
            {
                name = name.Substring(0, index);
            }

            // Trim "Grain" suffix
            index = name.LastIndexOf(GrainSuffix);
            if (index > 0 && name.Length - index == GrainSuffix.Length)
            {
                name = name.Substring(0, index);
            }

            // Append the generic arity, eg typeof(MyListGrain<T>) would eventually become mylist`1
            if (type.IsGenericType)
            {
                name = name + '`' + type.GetGenericArguments().Length;
            }

            return GrainType.Create(name);
        }
    }
}
