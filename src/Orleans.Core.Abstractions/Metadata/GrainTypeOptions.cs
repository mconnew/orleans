using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Orleans.Serialization.Configuration;

namespace Orleans.Configuration
{
    /// <summary>
    /// Contains grain type descriptions.
    /// </summary>
    public class GrainTypeOptions
    {
        /// <summary>
        /// Gets a collection of metadata about grain classes.
        /// </summary>
        public HashSet<Type> Classes { get; } = new ();

        /// <summary>
        /// Gets a collection of metadata about grain interfaces.
        /// </summary>
        public HashSet<Type> Interfaces { get; } = new ();
    }

    internal sealed class DefaultGrainTypeOptionsProvider : IConfigureOptions<GrainTypeOptions>
    {
        private readonly TypeManifestOptions _typeManifestOptions;

        public DefaultGrainTypeOptionsProvider(IOptions<TypeManifestOptions> typeManifestOptions) => _typeManifestOptions = typeManifestOptions.Value;

        public void Configure(GrainTypeOptions options)
        {
            foreach (var type in _typeManifestOptions.Interfaces)
            {
                if (typeof(IAddressable).IsAssignableFrom(type))
                {
                    options.Interfaces.Add(type switch
                    {
                        { IsGenericType: true } => type.GetGenericTypeDefinition(),
                        _ => type
                    });
                }
            }

            foreach (var type in _typeManifestOptions.InterfaceImplementations)
            {
                if (typeof(Grain).IsAssignableFrom(type))
                {
                    options.Classes.Add(type switch
                    {
                        { IsGenericType: true } => type.GetGenericTypeDefinition(),
                        _ => type
                    });
                }
            }
        }
    }
}
