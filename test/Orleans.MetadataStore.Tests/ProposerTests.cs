using System;
using System.Threading.Channels;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.MetadataStore.Tests
{
    [Trait("Category", "BVT"), Trait("Category", "MetadataStore")]
    public class ProposerTests
    {
        private const string Key = "key";
        private readonly Func<ExpandedReplicaSetConfiguration> getReplicaSetConfiguration;
        private readonly Proposer<int> proposer;

        public ProposerTests(ITestOutputHelper output)
        {
            this.proposer = new Proposer<int>(
                Key,
                Ballot.Zero,
                this.getReplicaSetConfiguration,
                new XunitLogger(output, "Proposer"));
        }
    }
}
