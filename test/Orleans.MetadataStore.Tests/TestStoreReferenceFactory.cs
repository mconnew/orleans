using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Orleans.Runtime;
using Xunit;

namespace Orleans.MetadataStore.Tests
{
    public class TestStoreReferenceFactory : IStoreReferenceFactory
    {
        public ConcurrentDictionary<SiloAddress, TestRemoteMetadataStore> Instances { get; } = new ConcurrentDictionary<SiloAddress, TestRemoteMetadataStore>();

        public Action<TestRemoteMetadataStore> OnCreateStore { get; set; }

        public SiloAddress GetAddress(IRemoteMetadataStore remoteStore) => ((TestRemoteMetadataStore)remoteStore).SiloAddress;

        public IRemoteMetadataStore GetReference(SiloAddress address, short instanceNum) => this.Instances.GetOrAdd(address, k => this.CreateStore(k));

        private TestRemoteMetadataStore CreateStore(SiloAddress address)
        {
            var result = new TestRemoteMetadataStore(address);
            this.OnCreateStore?.Invoke(result);
            return result;
        }
    }
}
