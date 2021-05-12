using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.Serialization;

namespace Orleans.MetadataStore.Storage
{
    public class MemoryLocalStore : ILocalStore
    {
        private readonly ConcurrentDictionary<string, object> _lookup = new();
        private readonly DeepCopier _copier;

        public MemoryLocalStore(DeepCopier copier)
        {
            _copier = copier;
        }

        public ValueTask<TValue> Read<TValue>(string key)
        {
            if (_lookup.TryGetValue(key, out var value))
            {
                return new ValueTask<TValue>((TValue)value);
            }

            return new ValueTask<TValue>(default(TValue));
        }

        public ValueTask Write<TValue>(string key, TValue value)
        {
            _lookup[key] = _copier.Copy(value);
            return default;
        }

        public ValueTask<List<string>> GetKeys(int maxResults = 100, string afterKey = null)
        {
            var include = afterKey == null;
            var results = new List<string>();
            foreach (var pair in _lookup)
            {
                if (include)
                {
                    results.Add(pair.Key);
                }

                if (string.Equals(pair.Key, afterKey, StringComparison.Ordinal))
                {
                    include = true;
                }

                if (results.Count >= maxResults)
                {
                    break;
                }
            }

            return new ValueTask<List<string>>(results);
        }
    }
}
