using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Orleans.Serialization;

namespace Orleans.MetadataStore.Storage
{
    public class SimpleFileSystemStore : ILocalStore
    {
        private readonly SimpleFileSystemStoreOptions _options;
        private readonly string _tmpDirectory;
        private readonly string _bakDirectory;
        private readonly Serializer _serializer;
        private readonly string _directory;

        public SimpleFileSystemStore(IOptions<SimpleFileSystemStoreOptions> options, Serializer serializer)
        {
            _options = options.Value;
            _directory = _options.Directory;
            _tmpDirectory = Path.Combine(_directory, "tmp");
            _bakDirectory = Path.Combine(_directory, "bak");
            _serializer = serializer;
            if (!Directory.Exists(_directory))
            {
                Directory.CreateDirectory(_directory);
            }

            if (!Directory.Exists(_tmpDirectory))
            {
                Directory.CreateDirectory(_tmpDirectory);
            }

            if (!Directory.Exists(_bakDirectory))
            {
                Directory.CreateDirectory(_bakDirectory);
            }
        }

        public async ValueTask<TValue> Read<TValue>(string key)
        {
            var path = Path.Combine(_directory, key + ".bin");
            if (!File.Exists(path))
            {
                return default;
            }

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            return _serializer.Deserialize<TValue>(memoryStream);
        }

        public async ValueTask Write<TValue>(string key, TValue value)
        {
            var fileName = key + ".bin";
            var targetFile = Path.Combine(_directory, fileName);
            using var stream = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.Write, 4096, FileOptions.Asynchronous);
            using var memoryStream = new MemoryStream();
            _serializer.Serialize(value, memoryStream);
            await memoryStream.CopyToAsync(stream);
            await stream.FlushAsync();
            stream.Close();
        }

        public ValueTask<List<string>> GetKeys(int maxResults = 100, string afterKey = null)
        {
            var keys = Directory.EnumerateFiles(_directory, "*.bin").Select(f => f.Remove(f.Length - 5));
            return new ValueTask<List<string>>(keys.ToList());
        }
    }
}