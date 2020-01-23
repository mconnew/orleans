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
        private readonly Action<RegisterState<int>> onUpdateState;
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
            this.onUpdateState = s => Assert.True(this.acceptorStates.Writer.TryWrite(s), "TryWrite failed");
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

            Assert.Null(acceptorInternal.PrivateState);
            Assert.False(this.acceptorStates.Reader.TryRead(out _));

            await ((Acceptor<int>.ITestAccessor)this.acceptor).EnsureStateLoaded();
            Assert.Equal(expectedInitialState, acceptorInternal.PrivateState);

            Assert.True(this.acceptorStates.Reader.TryRead(out var initialRegisterState));
            Assert.Equal(expectedInitialState, initialRegisterState);
        }

        [Fact]
        public async Task PrepareRejectsSupersededParentBallot()
        {
            this.acceptorParentBallot = new Ballot(2, 6);
            var acceptorInternal = (Acceptor<int>.ITestAccessor)this.acceptor;

            // The acceptor has a higher parent ballot than the proposer
            acceptorInternal.PrivateState = new RegisterState<int>(promised: Ballot.Zero, accepted: new Ballot(3, 4), value: 42);
            var response = await this.acceptor.Prepare(proposerParentBallot: new Ballot(1, 7), ballot: new Ballot(2, 7));
            var conflict = Assert.IsType<PrepareConfigConflict>(response);
            Assert.Equal(conflict.Conflicting, this.acceptorParentBallot);

            // No change
            var expected = new RegisterState<int>(promised: Ballot.Zero, accepted: new Ballot(3, 4), value: 42);
            Assert.False(this.acceptorStates.Reader.TryRead(out _));
            Assert.False(this.store.Values.ContainsKey(Key));
            Assert.Equal(expected.Promised, acceptorInternal.PrivateState.Promised);
            Assert.Equal(expected.Accepted, acceptorInternal.PrivateState.Accepted);
            Assert.Equal(expected.Value, acceptorInternal.PrivateState.Value);
        }

        [Fact]
        public async Task PrepareRejectsSupersededBallot()
        {
            this.acceptorParentBallot = new Ballot(2, 6);
            var acceptorInternal = (Acceptor<int>.ITestAccessor)this.acceptor;

            // The acceptor has a higher accepted ballot than the proposer, which results in a rejection.
            acceptorInternal.PrivateState = new RegisterState<int>(promised: Ballot.Zero, accepted: new Ballot(3, 4), value: 42);
            var response = await this.acceptor.Prepare(proposerParentBallot: this.acceptorParentBallot, ballot: new Ballot(2, 7));
            var conflict = Assert.IsType<PrepareConflict>(response);
            Assert.Equal(conflict.Conflicting, new Ballot(3, 4));

            // No change
            var expected = new RegisterState<int>(promised: Ballot.Zero, accepted: new Ballot(3, 4), value: 42);
            Assert.False(this.acceptorStates.Reader.TryRead(out _));
            Assert.False(this.store.Values.ContainsKey(Key));
            Assert.Equal(expected.Promised, acceptorInternal.PrivateState.Promised);
            Assert.Equal(expected.Accepted, acceptorInternal.PrivateState.Accepted);
            Assert.Equal(expected.Value, acceptorInternal.PrivateState.Value);

            // The acceptor has a higher promised ballot than the proposer, which results in a rejection.
            acceptorInternal.PrivateState = new RegisterState<int>(promised: new Ballot(3, 4), accepted: Ballot.Zero, value: 42);
            response = await this.acceptor.Prepare(proposerParentBallot: this.acceptorParentBallot, ballot: new Ballot(2, 7));
            conflict = Assert.IsType<PrepareConflict>(response);
            Assert.Equal(conflict.Conflicting, new Ballot(3, 4));

            // No change
            expected = new RegisterState<int>(promised: new Ballot(3, 4), accepted: Ballot.Zero, value: 42);
            Assert.False(this.acceptorStates.Reader.TryRead(out _));
            Assert.False(this.store.Values.ContainsKey(Key));
            Assert.Equal(expected.Promised, acceptorInternal.PrivateState.Promised);
            Assert.Equal(expected.Accepted, acceptorInternal.PrivateState.Accepted);
            Assert.Equal(expected.Value, acceptorInternal.PrivateState.Value);
        }

        [Fact]
        public async Task PrepareAcceptsSupersedingBallot()
        {
            this.acceptorParentBallot = new Ballot(2, 6);
            var acceptorInternal = (Acceptor<int>.ITestAccessor)this.acceptor;

            acceptorInternal.PrivateState = new RegisterState<int>(promised: Ballot.Zero, accepted: new Ballot(3, 4), value: 42);
            var response = await this.acceptor.Prepare(proposerParentBallot: this.acceptorParentBallot, ballot: new Ballot(3, 7));
            var success = Assert.IsType<PrepareSuccess<int>>(response);
            Assert.Equal(42, success.Value);
            Assert.Equal(new Ballot(3, 4), success.Accepted);
            Assert.False(this.acceptorStates.Reader.TryRead(out _));
            var writtenState = (RegisterState<int>)this.store.Values[Key];
            Assert.Equal(new Ballot(3, 7), acceptorInternal.PrivateState.Promised);
            Assert.Equal(new Ballot(3, 7), writtenState.Promised);
        }

        [Fact]
        public async Task AcceptRejectsSupersededParentBallot()
        {
            this.acceptorParentBallot = new Ballot(2, 6);
            var acceptorInternal = (Acceptor<int>.ITestAccessor)this.acceptor;

            // The acceptor has a higher parent ballot than the proposer
            acceptorInternal.PrivateState = new RegisterState<int>(promised: new Ballot(2, 7), accepted: Ballot.Zero, value: 42);
            var response = await this.acceptor.Accept(proposerParentBallot: new Ballot(1, 7), ballot: new Ballot(2, 7), 43);
            var conflict = Assert.IsType<AcceptConfigConflict>(response);
            Assert.Equal(conflict.Conflicting, this.acceptorParentBallot);

            // No change
            var expected = new RegisterState<int>(promised: new Ballot(2, 7), accepted: Ballot.Zero, value: 42);
            Assert.False(this.acceptorStates.Reader.TryRead(out _));
            Assert.False(this.store.Values.ContainsKey(Key));
            Assert.Equal(expected.Promised, acceptorInternal.PrivateState.Promised);
            Assert.Equal(expected.Accepted, acceptorInternal.PrivateState.Accepted);
            Assert.Equal(expected.Value, acceptorInternal.PrivateState.Value);
        }

        [Fact]
        public async Task AcceptRejectsSupersededBallot()
        {
            this.acceptorParentBallot = new Ballot(2, 6);
            var acceptorInternal = (Acceptor<int>.ITestAccessor)this.acceptor;

            // The acceptor has a higher accepted ballot than the proposer, which results in a rejection.
            acceptorInternal.PrivateState = new RegisterState<int>(promised: Ballot.Zero, accepted: new Ballot(3, 4), value: 42);
            var response = await this.acceptor.Accept(proposerParentBallot: this.acceptorParentBallot, ballot: new Ballot(2, 7), 43);
            var conflict = Assert.IsType<AcceptConflict>(response);
            Assert.Equal(conflict.Conflicting, new Ballot(3, 4));

            // No change
            var expected = new RegisterState<int>(promised: Ballot.Zero, accepted: new Ballot(3, 4), value: 42);
            Assert.False(this.acceptorStates.Reader.TryRead(out _));
            Assert.False(this.store.Values.ContainsKey(Key));
            Assert.Equal(expected.Promised, acceptorInternal.PrivateState.Promised);
            Assert.Equal(expected.Accepted, acceptorInternal.PrivateState.Accepted);
            Assert.Equal(expected.Value, acceptorInternal.PrivateState.Value);

            // The acceptor has a higher promised ballot than the proposer, which results in a rejection.
            acceptorInternal.PrivateState = new RegisterState<int>(promised: new Ballot(3, 4), accepted: Ballot.Zero, value: 42);
            response = await this.acceptor.Accept(proposerParentBallot: this.acceptorParentBallot, ballot: new Ballot(2, 7), 43);
            conflict = Assert.IsType<AcceptConflict>(response);
            Assert.Equal(conflict.Conflicting, new Ballot(3, 4));

            // No change
            expected = new RegisterState<int>(promised: new Ballot(3, 4), accepted: Ballot.Zero, value: 42);
            Assert.False(this.acceptorStates.Reader.TryRead(out _));
            Assert.False(this.store.Values.ContainsKey(Key));
            Assert.Equal(expected.Promised, acceptorInternal.PrivateState.Promised);
            Assert.Equal(expected.Accepted, acceptorInternal.PrivateState.Accepted);
            Assert.Equal(expected.Value, acceptorInternal.PrivateState.Value);
        }

        [Fact]
        public async Task AcceptAcceptsPromisedBallot()
        {
            this.acceptorParentBallot = new Ballot(2, 6);
            var acceptorInternal = (Acceptor<int>.ITestAccessor)this.acceptor;

            acceptorInternal.PrivateState = new RegisterState<int>(promised: new Ballot(3, 4), accepted: Ballot.Zero, value: 42);
            var response = await this.acceptor.Accept(this.acceptorParentBallot, new Ballot(3, 4), 43);
            Assert.IsType<AcceptSuccess>(response);
            Assert.True(this.acceptorStates.Reader.TryRead(out var updatedState));
            var writtenState = (RegisterState<int>)this.store.Values[Key];

            var expected = new RegisterState<int>(new Ballot(3, 4), new Ballot(3, 4), 43);
            Assert.Equal(expected.Promised, acceptorInternal.PrivateState.Promised);
            Assert.Equal(expected.Accepted, acceptorInternal.PrivateState.Accepted);
            Assert.Equal(expected.Value, acceptorInternal.PrivateState.Value);

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
