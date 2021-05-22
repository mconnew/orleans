using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Orleans.Runtime;
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
        private readonly SiloAddress[] silos;
        private readonly List<TestRemoteMetadataStore> remoteStores;
        private readonly ChangeFunction<int, int> permitIncrement = (existing, val) => val == existing + 1 ? val : existing;
        private ReplicaSetConfiguration config;
        private MetadataStoreOptions options;
        private TestStoreReferenceFactory referenceFactory;

        public ProposerTests(ITestOutputHelper output)
        {
            this.getReplicaSetConfiguration = () => ExpandedReplicaSetConfiguration.Create(this.config, this.options, this.referenceFactory);
            this.referenceFactory = new TestStoreReferenceFactory();
            this.silos = new[]
            {
                SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 1), 1),
                SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 2), 2),
                SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 3), 3)
            };
            this.remoteStores = new List<TestRemoteMetadataStore>(3);
            foreach (var silo in this.silos)
            {
                var store = (TestRemoteMetadataStore)this.referenceFactory.GetReference(silo, 0);
                this.remoteStores.Add(store);
            }
            this.options = new MetadataStoreOptions { InstancesPerSilo = 2 };
            this.config = new ReplicaSetConfiguration(new Ballot(2, 2), 1, this.silos, 2, 2);
            this.proposer = new Proposer<int>(
                Key,
                Ballot.Zero,
                this.getReplicaSetConfiguration,
                new XunitLogger(output, "Proposer"));
        }

        [Fact]
        public async Task TryUpdateSucceeds()
        {
            var proposerAccessor = (Proposer<int>.ITestAccessor)this.proposer;

            foreach (var store in this.remoteStores)
            {
                store.OnPrepare = (args) => new ValueTask<PrepareResponse<object>>(PrepareResponse.Success<object>(new Ballot(1, 1), 42));
                store.OnAccept = (args) => new ValueTask<AcceptResponse>(AcceptResponse.Success());
            }

            proposerAccessor.Ballot = new Ballot(2, 1);
            var expectedBallot = proposerAccessor.Ballot.Successor();

            var result = await this.proposer.TryUpdate(43, this.permitIncrement, CancellationToken.None);
            await this.AssertSuccess(expectedValue: 43, expectedBallot: expectedBallot);

            // Now try calling again. The 'distinguished leader' optimization should allow us to avoid the prepare round.
            expectedBallot = proposerAccessor.Ballot.Successor();
            result = await this.proposer.TryUpdate(44, this.permitIncrement, CancellationToken.None);
            Assert.Equal(ReplicationStatus.Success, result.Status);
            Assert.Equal(44, result.Value);
            await this.AssertDistinguishedLeaderSuccess(44, expectedBallot);
        }

        [Fact]
        public async Task TryUpdateRequiresPrepareQuorum()
        {
            var proposerAccessor = (Proposer<int>.ITestAccessor)this.proposer;

            this.remoteStores[0].OnPrepare = (args) => new ValueTask<PrepareResponse<object>>(PrepareResponse<object>.Success(new Ballot(1, 1), 42));
            this.remoteStores[0].OnAccept = (args) => new ValueTask<AcceptResponse>(AcceptResponse.Success());

            this.remoteStores[1].OnPrepare = (args) => new ValueTask<PrepareResponse<object>>(PrepareResponse<object>.Success(new Ballot(1, 1), 42));
            this.remoteStores[1].OnAccept = (args) => new ValueTask<AcceptResponse>(AcceptResponse.Success());

            // Conflict!
            this.remoteStores[2].OnPrepare = (args) => new ValueTask<PrepareResponse<object>>(PrepareResponse<object>.Conflict(new Ballot(3, 1)));
            this.remoteStores[2].OnAccept = (args) => new ValueTask<AcceptResponse>(AcceptResponse.Success());

            proposerAccessor.Ballot = new Ballot(2, 1);
            var expectedBallot = proposerAccessor.Ballot.Successor();

            var result = await this.proposer.TryUpdate(43, this.permitIncrement, CancellationToken.None);
            Assert.Equal(ReplicationStatus.Success, result.Status);
            Assert.Equal(43, result.Value);

            await this.AssertSuccess(expectedValue: 43, expectedBallot: expectedBallot);

            this.remoteStores[0].OnPrepare = (args) => new ValueTask<PrepareResponse<object>>(PrepareResponse<object>.Success(new Ballot(1, 2), 99));
            this.remoteStores[0].OnAccept = (args) => new ValueTask<AcceptResponse>(AcceptResponse.Success());

            // Conflict!
            this.remoteStores[1].OnPrepare = (args) => new ValueTask<PrepareResponse<object>>(PrepareResponse<object>.Conflict(new Ballot(7, 2)));
            this.remoteStores[1].OnAccept = (args) => new ValueTask<AcceptResponse>(AcceptResponse.Conflict(new Ballot(3, 2)));

            this.remoteStores[2].OnPrepare = (args) => new ValueTask<PrepareResponse<object>>(PrepareResponse<object>.Conflict(new Ballot(7, 2)));
            this.remoteStores[2].OnAccept = (args) => new ValueTask<AcceptResponse>(AcceptResponse.Conflict(new Ballot(3, 2)));

            proposerAccessor.Ballot = new Ballot(2, 1);
            expectedBallot = proposerAccessor.Ballot.Successor();

            proposerAccessor.SkipPrepare = false;
            result = await this.proposer.TryUpdate(43, this.permitIncrement, CancellationToken.None);
            Assert.Equal(ReplicationStatus.Failed, result.Status);
            Assert.Equal(99, result.Value);

            foreach (var store in this.remoteStores)
            {
                Assert.True(await store.PrepareCalls.WaitToReadAsync());
                Assert.True(store.PrepareCalls.TryRead(out var prepareArgs));
                Assert.Equal(this.config.Stamp, prepareArgs.ProposerParentBallot);
                Assert.Equal(Key, prepareArgs.Key);
                Assert.Equal(expectedBallot, prepareArgs.Ballot);

                Assert.True(await store.PrepareCalls.WaitToReadAsync());
                Assert.True(store.PrepareCalls.TryRead(out prepareArgs));
                Assert.Equal(this.config.Stamp, prepareArgs.ProposerParentBallot);
                Assert.Equal(Key, prepareArgs.Key);

                // Fast-forward to the new ballot
                Assert.Equal(new Ballot(8, 1), prepareArgs.Ballot);

                // Accept should not be called
                Assert.False(store.AcceptCalls.TryRead(out _));
            }
        }

        [Fact]
        public async Task TryUpdateRequiresPrepareQuorum_HardFailure()
        {
            var proposerAccessor = (Proposer<int>.ITestAccessor)this.proposer;

            foreach (var store in this.remoteStores)
            {
                store.OnPrepare = (args) => new ValueTask<PrepareResponse<object>>(Task.FromException<PrepareResponse<object>>(new Exception("nope!")));
                store.OnAccept = (args) => new ValueTask<AcceptResponse>(Task.FromException<AcceptResponse>(new Exception("nope!")));
            }

            proposerAccessor.Ballot = new Ballot(2, 1);
            var expectedBallot1 = proposerAccessor.Ballot.Successor();
            var expectedBallot2 = expectedBallot1.Successor();

            var (status, value) = await this.proposer.TryUpdate(43, this.permitIncrement, CancellationToken.None);
            Assert.Equal(ReplicationStatus.Failed, status);
            Assert.Equal(0, value);

            foreach (var store in this.remoteStores)
            {
                Assert.True(await store.PrepareCalls.WaitToReadAsync());
                Assert.True(store.PrepareCalls.TryRead(out var prepareArgs));
                Assert.Equal(this.config.Stamp, prepareArgs.ProposerParentBallot);
                Assert.Equal(Key, prepareArgs.Key);
                Assert.Equal(expectedBallot1, prepareArgs.Ballot);

                // Allow for one retry
                Assert.True(await store.PrepareCalls.WaitToReadAsync());
                Assert.True(store.PrepareCalls.TryRead(out prepareArgs));
                Assert.Equal(this.config.Stamp, prepareArgs.ProposerParentBallot);
                Assert.Equal(Key, prepareArgs.Key);
                Assert.Equal(expectedBallot2, prepareArgs.Ballot);

                // Accept should not be called
                Assert.False(store.AcceptCalls.TryRead(out _));
            }
        }

        [Fact]
        public async Task TryUpdateRequiresAcceptQuorum_HardFailure()
        {
            var proposerAccessor = (Proposer<int>.ITestAccessor)this.proposer;

            foreach (var store in this.remoteStores)
            {
                store.OnPrepare = (args) => new ValueTask<PrepareResponse<object>>(PrepareResponse.Success<object>(new Ballot(1, 2), 42));
                store.OnAccept = (args) => new ValueTask<AcceptResponse>(Task.FromException<AcceptResponse>(new Exception("nope!")));
            }

            proposerAccessor.Ballot = new Ballot(2, 1);
            var expectedBallot1 = proposerAccessor.Ballot.Successor();
            var expectedBallot2 = expectedBallot1.Successor();

            var (status, value) = await this.proposer.TryUpdate(43, this.permitIncrement, CancellationToken.None);
            Assert.Equal(ReplicationStatus.Uncertain, status);
            Assert.Equal(42, value);

            foreach (var store in this.remoteStores)
            {
                Assert.True(await store.PrepareCalls.WaitToReadAsync());
                Assert.True(store.PrepareCalls.TryRead(out var prepareArgs));
                Assert.Equal(this.config.Stamp, prepareArgs.ProposerParentBallot);
                Assert.Equal(Key, prepareArgs.Key);
                Assert.Equal(expectedBallot1, prepareArgs.Ballot);

                // Accept should fail on the first call.
                Assert.True(await store.AcceptCalls.WaitToReadAsync());
                Assert.True(store.AcceptCalls.TryRead(out var acceptArgs));
                Assert.Equal(this.config.Stamp, acceptArgs.ProposerParentBallot);
                Assert.Equal(expectedBallot1, acceptArgs.Ballot);
                Assert.Equal(43, acceptArgs.Value);
                Assert.Equal(Key, acceptArgs.Key);

                // After Accept initially fails, we should retry from Prepare.
                Assert.True(await store.PrepareCalls.WaitToReadAsync());
                Assert.True(store.PrepareCalls.TryRead(out prepareArgs));
                Assert.Equal(this.config.Stamp, prepareArgs.ProposerParentBallot);
                Assert.Equal(Key, prepareArgs.Key);
                Assert.Equal(expectedBallot2, prepareArgs.Ballot);
            }
        }

        private async Task AssertSuccess(int expectedValue, Ballot expectedBallot)
        {
            foreach (var store in this.remoteStores)
            {
                Assert.True(await store.PrepareCalls.WaitToReadAsync());
                Assert.True(store.PrepareCalls.TryRead(out var prepareArgs));
                Assert.False(store.PrepareCalls.TryRead(out _));

                // Validate call arguments.
                Assert.Equal(this.config.Stamp, prepareArgs.ProposerParentBallot);
                Assert.Equal(Key, prepareArgs.Key);
                Assert.Equal(expectedBallot, prepareArgs.Ballot);

                Assert.True(await store.AcceptCalls.WaitToReadAsync());
                Assert.True(store.AcceptCalls.TryRead(out var acceptArgs));
                Assert.False(store.AcceptCalls.TryRead(out _));

                // Validate call arguments.
                Assert.Equal(this.config.Stamp, acceptArgs.ProposerParentBallot);
                Assert.Equal(expectedBallot, acceptArgs.Ballot);
                Assert.Equal(expectedValue, acceptArgs.Value);
                Assert.Equal(Key, acceptArgs.Key);
            }
        }

        private async Task AssertDistinguishedLeaderSuccess(int expectedValue, Ballot expectedBallot)
        {
            foreach (var store in this.remoteStores)
            {
                Assert.True(await store.AcceptCalls.WaitToReadAsync());
                Assert.True(store.AcceptCalls.TryRead(out var acceptArgs));
                Assert.False(store.AcceptCalls.TryRead(out _));

                // Validate call arguments.
                Assert.Equal(this.config.Stamp, acceptArgs.ProposerParentBallot);
                Assert.Equal(expectedBallot, acceptArgs.Ballot);
                Assert.Equal(expectedValue, acceptArgs.Value);
                Assert.Equal(Key, acceptArgs.Key);

                // Prepare is checked second because even though it would have been called first, we do not expect it to have been called at all.
                // So we wait until we know Accept has been called before checking the prepare.
                Assert.False(store.PrepareCalls.TryRead(out _));
            }
        }
    }
}
