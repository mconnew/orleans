using System.Collections.Generic;

namespace Orleans.Serialization.Configuration
{
    /// <inheritdoc />
    internal class ConfigurationHolder<TConfiguration> : IConfiguration<TConfiguration> where TConfiguration : class, new()
    {
        /// <inheritdoc />
        public ConfigurationHolder(IEnumerable<IConfigurationProvider<TConfiguration>> providers)
        {
            Value = new TConfiguration();
            foreach (var provider in providers)
            {
                provider.Configure(Value);
            }
        }

        /// <inheritdoc />
        public TConfiguration Value { get; }
    }
}