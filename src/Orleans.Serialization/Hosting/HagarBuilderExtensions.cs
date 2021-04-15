using Orleans.Serialization.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Orleans.Serialization
{
    public static class SerializerBuilderExtensions
    {
        private static readonly object _assembliesKey = new object();
        public static ISerializerBuilder AddProvider(this ISerializerBuilder builder, Func<IServiceProvider, IConfigurationProvider<SerializerConfiguration>> factory) => ((ISerializerBuilderImplementation)builder).ConfigureServices(services => services.AddTransient(sp => factory(sp)));

        public static ISerializerBuilder AddProvider(this ISerializerBuilder builder, IConfigurationProvider<SerializerConfiguration> provider) => ((ISerializerBuilderImplementation)builder).ConfigureServices(services => services.AddSingleton(provider));

        public static ISerializerBuilder Configure(this ISerializerBuilder builder, Action<SerializerConfiguration> configure) => builder.AddProvider(new DelegateConfigurationProvider<SerializerConfiguration>(configure));

        public static ISerializerBuilder AddAssembly(this ISerializerBuilder builder, Assembly assembly)
        {
            var properties = ((ISerializerBuilderImplementation)builder).Properties;
            HashSet<Assembly> assembliesSet;
            if (!properties.TryGetValue(_assembliesKey, out var assembliesSetObj))
            {
                assembliesSet = new HashSet<Assembly>();
                properties[_assembliesKey] = assembliesSet;
            }
            else
            {
                assembliesSet = (HashSet<Assembly>)assembliesSetObj;
            }
                
            if (!assembliesSet.Add(assembly))
            {
                return builder;
            }

            var attrs = assembly.GetCustomAttributes<MetadataProviderAttribute>();
            foreach (var attr in attrs)
            {
                if (!typeof(IConfigurationProvider<SerializerConfiguration>).IsAssignableFrom(attr.ProviderType))
                {
                    continue;
                }

                _ = builder.AddProvider(sp => (IConfigurationProvider<SerializerConfiguration>)ActivatorUtilities.GetServiceOrCreateInstance(sp, attr.ProviderType));
            }

            return builder;
        }

        private sealed class DelegateConfigurationProvider<TOptions> : IConfigurationProvider<TOptions>
        {
            private readonly Action<TOptions> _configure;

            public DelegateConfigurationProvider(Action<TOptions> configure)
            {
                _configure = configure;
            }

            public void Configure(TOptions configuration) => _configure(configuration);
        }
    }
}