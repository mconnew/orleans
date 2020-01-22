using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.MetadataStore.Storage;
using Xunit;

namespace Orleans.MetadataStore.Tests
{
    [Trait("Category", "BVT"), Trait("Category", "MetadataStore")]
    public class AcceptorTests
    {
        public async Task Prepare()
        {
            var store = new TestMemoryLocalStore();
            var getParentBallot = new Func<Ballot>[] { () => Ballot.Zero };
            var onUpdateState = new Action<RegisterState<int>>[] { register => { } };
            var acceptor = new Acceptor<int>(
                "key",
                store,
                () => getParentBallot[0](),
                x => onUpdateState[0](x),
                NullLogger.Instance);
            var proposer = new Proposer();
            proposer.TryUpdate
            acceptor.Prepare
                
        } 
    }

    public class TestMemoryLocalStore : ILocalStore
    {
        private readonly ConcurrentDictionary<string, object> lookup = new ConcurrentDictionary<string, object>();

        public ValueTask<TValue> Read<TValue>(string key)
        {
            if (this.lookup.TryGetValue(key, out var value))
            {
                return new ValueTask<TValue>((TValue)value);
            }

            return new ValueTask<TValue>(default(TValue));
        }

        public ValueTask Write<TValue>(string key, TValue value)
        {
            this.lookup[key] = value;
            return default;
        }

        public ValueTask<List<string>> GetKeys(int maxResults = 100, string afterKey = null)
        {
            var include = afterKey == null;
            var results = new List<string>();
            foreach (var pair in this.lookup)
            {
                if (include)
                {
                    results.Add(pair.Key);
                }

                if (string.Equals(pair.Key, afterKey, StringComparison.Ordinal)) include = true;
                if (results.Count >= maxResults) break;
            }

            return new ValueTask<List<string>>(results);
        }
    }
}
