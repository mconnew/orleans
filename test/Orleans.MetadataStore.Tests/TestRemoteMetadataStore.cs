using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Orleans.Runtime;
using Xunit;

namespace Orleans.MetadataStore.Tests
{
    public class TestRemoteMetadataStore : IRemoteMetadataStore
    {
        private readonly Channel<(string Key, Ballot ProposerParentBallot, Ballot Ballot)> prepareCallChannel = Channel.CreateUnbounded<(string Key, Ballot ProposerParentBallot, Ballot Ballot)>();
        private readonly Channel<(string Key, Ballot ProposerParentBallot, Ballot Ballot, object Value)> acceptCallChannel = Channel.CreateUnbounded<(string Key, Ballot ProposerParentBallot, Ballot Ballot, object Value)>();

        public ChannelReader<(string Key, Ballot ProposerParentBallot, Ballot Ballot)> PrepareCalls => this.prepareCallChannel.Reader;

        public ChannelReader<(string Key, Ballot ProposerParentBallot, Ballot Ballot, object Value)> AcceptCalls => this.acceptCallChannel.Reader;

        public TestRemoteMetadataStore(SiloAddress addr)
        {
            this.SiloAddress = addr;
        }

        public SiloAddress SiloAddress { get; }

        public Func<(string Key, Ballot ProposerParentBallot, Ballot Ballot, object Value), ValueTask<AcceptResponse>> OnAccept { get; set; }
        public Func<ValueTask<List<string>>> OnGetKeys { get; set; }
        public Func<(string Key, Ballot OroposerParentBallot, Ballot Ballot), ValueTask<PrepareResponse>> OnPrepare { get; set; }


        public ValueTask<AcceptResponse> Accept(string key, Ballot proposerParentBallot, Ballot ballot, object value)
        {
            var args = (key, proposerParentBallot, ballot, value);
            Assert.True(this.acceptCallChannel.Writer.TryWrite(args));
            return this.OnAccept?.Invoke(args) ?? default;
        }

        public ValueTask<List<string>> GetKeys() => this.OnGetKeys?.Invoke() ?? default;

        public ValueTask<PrepareResponse> Prepare(string key, Ballot proposerParentBallot, Ballot ballot)
        {
            var args = (key, proposerParentBallot, ballot);
            Assert.True(this.prepareCallChannel.Writer.TryWrite(args));
            return this.OnPrepare?.Invoke(args) ?? default;
        }
    }
}
