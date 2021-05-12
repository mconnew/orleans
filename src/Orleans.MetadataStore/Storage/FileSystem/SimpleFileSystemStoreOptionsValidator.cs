using System.IO;
using Microsoft.Extensions.Options;
using Orleans.Runtime;

namespace Orleans.MetadataStore.Storage
{
    public class SimpleFileSystemStoreOptionsValidator : IConfigurationValidator
    {
        private readonly IOptions<SimpleFileSystemStoreOptions> _options;
        public SimpleFileSystemStoreOptionsValidator(IOptions<SimpleFileSystemStoreOptions> options)
        {
            _options = options;
        }

        public void ValidateConfiguration()
        {
            var dir = _options.Value.Directory;
            if (string.IsNullOrWhiteSpace(dir))
            {
                throw new OrleansConfigurationException($"{nameof(SimpleFileSystemStoreOptions)}.{nameof(SimpleFileSystemStoreOptions.Directory)} must have a value.");
            }

            if (!Directory.GetParent(dir).Exists)
            {
                throw new OrleansConfigurationException($"The parent directory of {nameof(SimpleFileSystemStoreOptions)}.{nameof(SimpleFileSystemStoreOptions.Directory)} must exist.");
            }
        }
    }
}