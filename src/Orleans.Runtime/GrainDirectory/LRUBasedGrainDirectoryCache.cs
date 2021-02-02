using System;
using System.Collections.Generic;

namespace Orleans.Runtime.GrainDirectory
{
    internal class LRUBasedGrainDirectoryCache : IGrainDirectoryCache
    {
        private readonly LRU<GrainId, ActivationAddress> cache;

        public LRUBasedGrainDirectoryCache(int maxCacheSize, TimeSpan maxEntryAge)
        {
            cache = new LRU<GrainId, ActivationAddress>(maxCacheSize, maxEntryAge, null);
        }

        public void AddOrUpdate(GrainId key, ActivationAddress value)
        {
            cache.Add(key, value);
        }

        public bool Remove(ActivationAddress address)
        {
            return cache.RemoveKey(address.Grain, out _);
        }

        public void Clear()
        {
            cache.Clear();
        }

        public bool LookUp(GrainId key, out ActivationAddress result)
        {
            return cache.TryGetValue(key, out result);
        }

        public List<ActivationAddress> Entries
        {
            get
            {
                var result = new List<ActivationAddress>();
                var enumerator = cache.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    var value = current.Value;
                    result.Add(value);
                }
                
                return result;
            }
        }
    }
}
