using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Orleans.MetadataStore.Storage;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.MetadataStore.Tests
{
    [Trait("Category", "BVT"), Trait("Category", "MetadataStore")]
    public class AcceptorTests
    {
        private const string Key = "key";
        private readonly TestMemoryLocalStore store = new TestMemoryLocalStore();
        private readonly Func<Ballot> getParentBallot;
        private readonly Func<RegisterState<int>, ValueTask> onUpdateState;
        private readonly Channel<RegisterState<int>> acceptorStates = Channel.CreateUnbounded<RegisterState<int>>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = true
        });
        private readonly Acceptor<int> acceptor;

        private Ballot acceptorParentBallot = Ballot.Zero;        

        public AcceptorTests(ITestOutputHelper output)
        {
            this.getParentBallot = () => this.acceptorParentBallot;
            this.onUpdateState = s => { Assert.True(this.acceptorStates.Writer.TryWrite(s), "TryWrite failed"); return default; };
            this.acceptor = new Acceptor<int>(
                Key,
                this.store,
                this.getParentBallot,
                this.onUpdateState,
                new XunitLogger(output, "Acceptor"));
        }

        [Fact]
        public async Task AcceptorLoadsCorrectState()
        {
            var expectedInitialState = new RegisterState<int>(new Ballot(10, 2), new Ballot(3, 4), 42);
            var acceptorInternal = (Acceptor<int>.ITestAccessor)this.acceptor;
            await this.store.Write<RegisterState<int>>(Key, expectedInitialState);

            Assert.Null(acceptorInternal.VolatileState);
            Assert.False(this.acceptorStates.Reader.TryRead(out _));

            await ((Acceptor<int>.ITestAccessor)this.acceptor).EnsureStateLoaded();
            Assert.Equal(expectedInitialState, acceptorInternal.VolatileState);

            Assert.True(this.acceptorStates.Reader.TryRead(out var initialRegisterState));
            Assert.Equal(expectedInitialState, initialRegisterState);
        }

        [Fact]
        public async Task PrepareRejectsSupersededParentBallot()
        {
            this.acceptorParentBallot = new Ballot(2, 6);
            var acceptorInternal = (Acceptor<int>.ITestAccessor)this.acceptor;

            // The acceptor has a higher parent ballot than the proposer
            acceptorInternal.VolatileState = new RegisterState<int>(promised: Ballot.Zero, accepted: new Ballot(3, 4), value: 42);
            var response = await this.acceptor.Prepare(proposerParentBallot: new Ballot(1, 7), ballot: new Ballot(2, 7));
            Assert.Equal(PrepareStatus.ConfigConflict, response.Status);
            Assert.Equal(response.Ballot, this.acceptorParentBallot);

            // No change
            var expected = new RegisterState<int>(promised: Ballot.Zero, accepted: new Ballot(3, 4), value: 42);
            Assert.False(this.acceptorStates.Reader.TryRead(out _));
            Assert.False(this.store.Values.ContainsKey(Key));
            Assert.Equal(expected.Promised, acceptorInternal.VolatileState.Promised);
            Assert.Equal(expected.Accepted, acceptorInternal.VolatileState.Accepted);
            Assert.Equal(expected.Value, acceptorInternal.VolatileState.Value);
        }

        [Fact]
        public async Task PrepareRejectsSupersededBallot()
        {
            this.acceptorParentBallot = new Ballot(2, 6);
            var acceptorInternal = (Acceptor<int>.ITestAccessor)this.acceptor;

            // The acceptor has a higher accepted ballot than the proposer, which results in a rejection.
            acceptorInternal.VolatileState = new RegisterState<int>(promised: Ballot.Zero, accepted: new Ballot(3, 4), value: 42);
            var response = await this.acceptor.Prepare(proposerParentBallot: this.acceptorParentBallot, ballot: new Ballot(2, 7));
            Assert.Equal(PrepareStatus.Conflict, response.Status);
            Assert.Equal(response.Ballot, new Ballot(3, 4));

            // No change
            var expected = new RegisterState<int>(promised: Ballot.Zero, accepted: new Ballot(3, 4), value: 42);
            Assert.False(this.acceptorStates.Reader.TryRead(out _));
            Assert.False(this.store.Values.ContainsKey(Key));
            Assert.Equal(expected.Promised, acceptorInternal.VolatileState.Promised);
            Assert.Equal(expected.Accepted, acceptorInternal.VolatileState.Accepted);
            Assert.Equal(expected.Value, acceptorInternal.VolatileState.Value);

            // The acceptor has a higher promised ballot than the proposer, which results in a rejection.
            acceptorInternal.VolatileState = new RegisterState<int>(promised: new Ballot(3, 4), accepted: Ballot.Zero, value: 42);
            response = await this.acceptor.Prepare(proposerParentBallot: this.acceptorParentBallot, ballot: new Ballot(2, 7));
            Assert.Equal(PrepareStatus.Conflict, response.Status);
            Assert.Equal(response.Ballot, new Ballot(3, 4));

            // No change
            expected = new RegisterState<int>(promised: new Ballot(3, 4), accepted: Ballot.Zero, value: 42);
            Assert.False(this.acceptorStates.Reader.TryRead(out _));
            Assert.False(this.store.Values.ContainsKey(Key));
            Assert.Equal(expected.Promised, acceptorInternal.VolatileState.Promised);
            Assert.Equal(expected.Accepted, acceptorInternal.VolatileState.Accepted);
            Assert.Equal(expected.Value, acceptorInternal.VolatileState.Value);
        }

        [Fact]
        public async Task PrepareAcceptsSupersedingBallot()
        {
            this.acceptorParentBallot = new Ballot(2, 6);
            var acceptorInternal = (Acceptor<int>.ITestAccessor)this.acceptor;

            acceptorInternal.VolatileState = new RegisterState<int>(promised: Ballot.Zero, accepted: new Ballot(3, 4), value: 42);
            var response = await this.acceptor.Prepare(proposerParentBallot: this.acceptorParentBallot, ballot: new Ballot(3, 7));
            Assert.Equal(PrepareStatus.Success, response.Status);
            Assert.Equal(42, response.Value);
            Assert.Equal(new Ballot(3, 4), response.Ballot);
            Assert.False(this.acceptorStates.Reader.TryRead(out _));
            var writtenState = (RegisterState<int>)this.store.Values[Key];
            Assert.Equal(new Ballot(3, 7), acceptorInternal.VolatileState.Promised);
            Assert.Equal(new Ballot(3, 7), writtenState.Promised);
        }

        [Fact]
        public async Task AcceptRejectsSupersededParentBallot()
        {
            this.acceptorParentBallot = new Ballot(2, 6);
            var acceptorInternal = (Acceptor<int>.ITestAccessor)this.acceptor;

            // The acceptor has a higher parent ballot than the proposer
            acceptorInternal.VolatileState = new RegisterState<int>(promised: new Ballot(2, 7), accepted: Ballot.Zero, value: 42);
            var response = await this.acceptor.Accept(proposerParentBallot: new Ballot(1, 7), ballot: new Ballot(2, 7), 43);
            Assert.Equal(AcceptStatus.ConfigConflict, response.Status);
            Assert.Equal(response.Ballot, this.acceptorParentBallot);

            // No change
            var expected = new RegisterState<int>(promised: new Ballot(2, 7), accepted: Ballot.Zero, value: 42);
            Assert.False(this.acceptorStates.Reader.TryRead(out _));
            Assert.False(this.store.Values.ContainsKey(Key));
            Assert.Equal(expected.Promised, acceptorInternal.VolatileState.Promised);
            Assert.Equal(expected.Accepted, acceptorInternal.VolatileState.Accepted);
            Assert.Equal(expected.Value, acceptorInternal.VolatileState.Value);
        }

        [Fact]
        public async Task AcceptRejectsSupersededBallot()
        {
            this.acceptorParentBallot = new Ballot(2, 6);
            var acceptorInternal = (Acceptor<int>.ITestAccessor)this.acceptor;

            // The acceptor has a higher accepted ballot than the proposer, which results in a rejection.
            acceptorInternal.VolatileState = new RegisterState<int>(promised: Ballot.Zero, accepted: new Ballot(3, 4), value: 42);
            var response = await this.acceptor.Accept(proposerParentBallot: this.acceptorParentBallot, ballot: new Ballot(2, 7), 43);
            Assert.Equal(AcceptStatus.Conflict, response.Status);
            Assert.Equal(response.Ballot, new Ballot(3, 4));

            // No change
            var expected = new RegisterState<int>(promised: Ballot.Zero, accepted: new Ballot(3, 4), value: 42);
            Assert.False(this.acceptorStates.Reader.TryRead(out _));
            Assert.False(this.store.Values.ContainsKey(Key));
            Assert.Equal(expected.Promised, acceptorInternal.VolatileState.Promised);
            Assert.Equal(expected.Accepted, acceptorInternal.VolatileState.Accepted);
            Assert.Equal(expected.Value, acceptorInternal.VolatileState.Value);

            // The acceptor has a higher promised ballot than the proposer, which results in a rejection.
            acceptorInternal.VolatileState = new RegisterState<int>(promised: new Ballot(3, 4), accepted: Ballot.Zero, value: 42);
            response = await this.acceptor.Accept(proposerParentBallot: this.acceptorParentBallot, ballot: new Ballot(2, 7), 43);
            Assert.Equal(AcceptStatus.Conflict, response.Status);
            Assert.Equal(response.Ballot, new Ballot(3, 4));

            // No change
            expected = new RegisterState<int>(promised: new Ballot(3, 4), accepted: Ballot.Zero, value: 42);
            Assert.False(this.acceptorStates.Reader.TryRead(out _));
            Assert.False(this.store.Values.ContainsKey(Key));
            Assert.Equal(expected.Promised, acceptorInternal.VolatileState.Promised);
            Assert.Equal(expected.Accepted, acceptorInternal.VolatileState.Accepted);
            Assert.Equal(expected.Value, acceptorInternal.VolatileState.Value);
        }

        [Fact]
        public async Task AcceptAcceptsPromisedBallot()
        {
            this.acceptorParentBallot = new Ballot(2, 6);
            var acceptorInternal = (Acceptor<int>.ITestAccessor)this.acceptor;

            acceptorInternal.VolatileState = new RegisterState<int>(promised: new Ballot(3, 4), accepted: Ballot.Zero, value: 42);
            var response = await this.acceptor.Accept(this.acceptorParentBallot, new Ballot(3, 4), 43);
            Assert.Equal(AcceptStatus.Success, response.Status);
            Assert.True(this.acceptorStates.Reader.TryRead(out var updatedState));
            var writtenState = (RegisterState<int>)this.store.Values[Key];

            var expected = new RegisterState<int>(new Ballot(3, 4), new Ballot(3, 4), 43);
            Assert.Equal(expected.Promised, acceptorInternal.VolatileState.Promised);
            Assert.Equal(expected.Accepted, acceptorInternal.VolatileState.Accepted);
            Assert.Equal(expected.Value, acceptorInternal.VolatileState.Value);

            Assert.Equal(expected.Promised, writtenState.Promised);
            Assert.Equal(expected.Accepted, writtenState.Accepted);
            Assert.Equal(expected.Value, writtenState.Value);

            Assert.Equal(expected.Promised, updatedState.Promised);
            Assert.Equal(expected.Accepted, updatedState.Accepted);
            Assert.Equal(expected.Value, updatedState.Value);
        }
    }

    public class TestMemoryLocalStore : ILocalStore
    {
        public ConcurrentDictionary<string, object> Values { get; } = new ConcurrentDictionary<string, object>();

        public ValueTask<TValue> Read<TValue>(string key)
        {
            if (this.Values.TryGetValue(key, out var value))
            {
                return new ValueTask<TValue>((TValue)value);
            }

            return new ValueTask<TValue>(default(TValue));
        }

        public ValueTask Write<TValue>(string key, TValue value)
        {
            this.Values[key] = value;
            return default;
        }

        public ValueTask<List<string>> GetKeys(int maxResults = 100, string afterKey = null)
        {
            var include = afterKey == null;
            var results = new List<string>();
            foreach (var pair in this.Values)
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
