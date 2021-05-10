using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.MetadataStore.Storage
{
    public interface ILocalStore
    {
        ValueTask<TValue> Read<TValue>(string key);
        ValueTask Write<TValue>(string key, TValue value);
        ValueTask<List<string>> GetKeys(int maxResults = 100, string afterKey = null);
    }
}