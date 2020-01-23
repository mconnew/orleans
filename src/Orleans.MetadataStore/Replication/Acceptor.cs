using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.MetadataStore.Storage;
using AsyncEx = Nito.AsyncEx;

namespace Orleans.MetadataStore
{
    public class Acceptor<TValue> : IAcceptor<TValue>, Acceptor<TValue>.ITestAccessor
    {
        private readonly AsyncEx.AsyncLock lockObj;
        private readonly ILocalStore store;
        private readonly Func<Ballot> getParentBallot;
        private readonly Action<RegisterState<TValue>> onUpdateState;
        private readonly string key;
        private readonly ILogger log;
        private RegisterState<TValue> state;

        RegisterState<TValue> ITestAccessor.PrivateState { get => this.state; set => this.state = value; }

        public Acceptor(
            string key,
            ILocalStore store,
            Func<Ballot> getParentBallot,
            Action<RegisterState<TValue>> onUpdateState,
            ILogger log)
        {
            this.lockObj = new AsyncEx.AsyncLock();
            this.key = key;
            this.store = store;
            this.getParentBallot = getParentBallot;
            this.onUpdateState = onUpdateState;
            this.log = log;
        }
        
        public async ValueTask<PrepareResponse> Prepare(Ballot proposerParentBallot, Ballot ballot)
        {
            using (await this.lockObj.LockAsync())
            {
                // Initialize the register state if it's not yet initialized.
                await this.EnsureStateLoadedNoLock();

                PrepareResponse result;
                var parentBallot = this.getParentBallot();
                if (parentBallot > proposerParentBallot)
                {
                    // If the proposer is using a cluster configuration version which is lower than the highest
                    // cluster configuration version observed by this node, then the Prepare is rejected.
                    result = PrepareResponse.ConfigConflict(parentBallot);
                }
                else
                {
                    if (this.state.Promised > ballot)
                    {
                        // If a Prepare with a higher ballot has already been encountered, reject this.
                        result = PrepareResponse.Conflict(this.state.Promised);
                    }
                    else if (this.state.Accepted > ballot)
                    {
                        // If an Accept with a higher ballot has already been encountered, reject this.
                        result = PrepareResponse.Conflict(this.state.Accepted);
                    }
                    else
                    {
                        // Record a tentative promise to accept this proposer's value.
                        var newState = new RegisterState<TValue>(ballot, this.state.Accepted, this.state.Value);
                        await this.store.Write(this.key, newState);
                        this.state = newState;

                        result = PrepareResponse.Success(this.state.Accepted, this.state.Value);
                    }
                }

                LogPrepare(proposerParentBallot, ballot, result);
                return result;
            }
        }

        public async ValueTask<AcceptResponse> Accept(Ballot proposerParentBallot, Ballot ballot, TValue value)
        {
            using (await this.lockObj.LockAsync())
            {
                // Initialize the register state if it's not yet initialized.
                await this.EnsureStateLoadedNoLock();

                AcceptResponse result;
                var parentBallot = this.getParentBallot();
                if (parentBallot > proposerParentBallot)
                {
                    // If the proposer is using a cluster configuration version which is lower than the highest
                    // cluster configuration version observed by this node, then the Accept is rejected.
                    result = AcceptResponse.ConfigConflict(parentBallot);
                }
                else
                {
                    if (this.state.Promised > ballot)
                    {
                        // If a Prepare with a higher ballot has already been encountered, reject this.
                        result = AcceptResponse.Conflict(this.state.Promised);
                    }
                    else if (this.state.Accepted > ballot)
                    {
                        // If an Accept with a higher ballot has already been encountered, reject this.
                        result = AcceptResponse.Conflict(this.state.Accepted);
                    }
                    else
                    {
                        // Record the new state.
                        var newState = new RegisterState<TValue>(ballot, ballot, value);
                        await this.store.Write(this.key, newState);
                        this.state = newState;
                        this.onUpdateState?.Invoke(this.state);
                        result = AcceptResponse.Success();
                    }
                }

                LogAccept(ballot, value, result);
                return result;
            }
        }

        internal async Task ForceState(TValue newState)
        {
            using (await this.lockObj.LockAsync())
            {
                this.state = new RegisterState<TValue>(Ballot.Zero, Ballot.Zero, newState);
                this.onUpdateState?.Invoke(this.state);
            }
        }

        private void LogPrepare(Ballot parentBallot, Ballot ballot, PrepareResponse result)
        {
            if (this.log.IsEnabled(LogLevel.Trace))
            {
                this.log.LogTrace("Prepare(parentBallot: {ParentBallot}, ballot: {Ballot}) -> {Result}", parentBallot, ballot, result);
            }
        }

        private void LogAccept(Ballot ballot, TValue value, AcceptResponse result)
        {
            if (this.log.IsEnabled(LogLevel.Trace))
            {
                this.log.LogTrace("Accept({Ballot}, {Value}) -> {Result}", ballot, value, result);
            }
        }

        Task ITestAccessor.EnsureStateLoaded() => this.EnsureStateLoaded();

        internal async Task EnsureStateLoaded()
        {
            using (await this.lockObj.LockAsync())
            {
                await EnsureStateLoadedNoLock();
            }
        }

        private ValueTask EnsureStateLoadedNoLock()
        {
            if (this.state is object) return default;

            return EnsureStateLoadedNoLockAsync();

            async ValueTask EnsureStateLoadedNoLockAsync()
            {
                var stored = await this.store.Read<RegisterState<TValue>>(this.key);
                this.state = stored ?? RegisterState<TValue>.Default;
                this.onUpdateState?.Invoke(this.state);

                if (this.log.IsEnabled(LogLevel.Trace))
                {
                    this.log.LogTrace("Initialized with register state {State}", this.state);
                }
            }
        }

        public interface ITestAccessor
        {
            Task EnsureStateLoaded();

            RegisterState<TValue> PrivateState { get; set; }
        }
    }
}