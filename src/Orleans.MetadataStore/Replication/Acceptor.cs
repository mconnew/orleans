using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.MetadataStore.Storage;
using AsyncEx = Nito.AsyncEx;

namespace Orleans.MetadataStore
{
    public class Acceptor<TValue> : IAcceptor<TValue>, Acceptor<TValue>.ITestAccessor
    {
        private readonly AsyncEx.AsyncLock _lockObj;
        private readonly ILocalStore _store;
        private readonly Func<Ballot> _getParentBallot;
        private readonly Func<RegisterState<TValue>, ValueTask> _onUpdateState;
        private readonly string _key;
        private readonly ILogger _log;
        private RegisterState<TValue> _state;
        
        public Acceptor(
            string key,
            ILocalStore store,
            Func<Ballot> getParentBallot,
            Func<RegisterState<TValue>, ValueTask> onUpdateState,
            ILogger log)
        {
            _lockObj = new AsyncEx.AsyncLock();
            _key = key;
            _store = store;
            _getParentBallot = getParentBallot;
            _onUpdateState = onUpdateState;
            _log = log;
        }

        RegisterState<TValue> ITestAccessor.VolatileState { get => _state; set => _state = value; }

        public async ValueTask<PrepareResponse> Prepare(Ballot proposerParentBallot, Ballot ballot)
        {
            using (await _lockObj.LockAsync())
            {
                // Initialize the register state if it's not yet initialized.
                await EnsureStateLoadedNoLock();

                PrepareResponse result;
                var parentBallot = _getParentBallot();
                if (parentBallot > proposerParentBallot)
                {
                    // If the proposer is using a cluster configuration version which is lower than the highest
                    // cluster configuration version observed by this node, then the Prepare is rejected.
                    result = PrepareResponse.ConfigConflict(parentBallot);
                }
                else
                {
                    if (_state.Promised > ballot)
                    {
                        // If a Prepare with a higher ballot has already been encountered, reject this.
                        result = PrepareResponse.Conflict(_state.Promised);
                    }
                    else if (_state.Accepted > ballot)
                    {
                        // If an Accept with a higher ballot has already been encountered, reject this.
                        result = PrepareResponse.Conflict(_state.Accepted);
                    }
                    else
                    {
                        // Record a tentative promise to accept this proposer's value.
                        var newState = new RegisterState<TValue>(ballot, _state.Accepted, _state.Value);
                        await _store.Write(_key, newState);
                        _state = newState;

                        result = PrepareResponse.Success(_state.Accepted, _state.Value);
                    }
                }

                LogPrepare(proposerParentBallot, ballot, result);
                return result;
            }
        }

        public async ValueTask<AcceptResponse> Accept(Ballot proposerParentBallot, Ballot ballot, TValue value)
        {
            using (await _lockObj.LockAsync())
            {
                // Initialize the register state if it's not yet initialized.
                await EnsureStateLoadedNoLock();

                AcceptResponse result;
                var parentBallot = _getParentBallot();
                if (parentBallot > proposerParentBallot)
                {
                    // If the proposer is using a cluster configuration version which is lower than the highest
                    // cluster configuration version observed by this node, then the Accept is rejected.
                    result = AcceptResponse.ConfigConflict(parentBallot);
                }
                else
                {
                    if (_state.Promised > ballot)
                    {
                        // If a Prepare with a higher ballot has already been encountered, reject this.
                        result = AcceptResponse.Conflict(_state.Promised);
                    }
                    else if (_state.Accepted > ballot)
                    {
                        // If an Accept with a higher ballot has already been encountered, reject this.
                        result = AcceptResponse.Conflict(_state.Accepted);
                    }
                    else
                    {
                        // Record the new state.
                        var newState = new RegisterState<TValue>(ballot, ballot, value);
                        await _store.Write(_key, newState);
                        _state = newState;
                        if (_onUpdateState?.Invoke(_state) is ValueTask task)
                        {
                            await task;
                        }

                        result = AcceptResponse.Success();
                    }
                }

                LogAccept(ballot, value, result);
                return result;
            }
        }

        internal async Task ForceState(TValue newState)
        {
            using (await _lockObj.LockAsync())
            {
                _state = new RegisterState<TValue>(Ballot.Zero, Ballot.Zero, newState);
                if (_onUpdateState?.Invoke(_state) is ValueTask task)
                {
                    await task;
                }
            }
        }

        [Conditional("DEBUG")]
        private void LogPrepare(Ballot parentBallot, Ballot ballot, PrepareResponse result)
        {
            if (_log.IsEnabled(LogLevel.Trace))
            {
                _log.LogTrace("Prepare(parentBallot: {ParentBallot}, ballot: {Ballot}) -> {Result}", parentBallot, ballot, result);
            }
        }

        [Conditional("DEBUG")]
        private void LogAccept(Ballot ballot, TValue value, AcceptResponse result)
        {
            if (_log.IsEnabled(LogLevel.Trace))
            {
                _log.LogTrace("Accept({Ballot}, {Value}) -> {Result}", ballot, value, result);
            }
        }

        Task ITestAccessor.EnsureStateLoaded() => EnsureStateLoaded();

        internal async Task EnsureStateLoaded()
        {
            using (await _lockObj.LockAsync())
            {
                await EnsureStateLoadedNoLock();
            }
        }

        private ValueTask EnsureStateLoadedNoLock()
        {
            if (_state is object)
            {
                return default;
            }

            return EnsureStateLoadedNoLockAsync();

            async ValueTask EnsureStateLoadedNoLockAsync()
            {
                var stored = await _store.Read<RegisterState<TValue>>(_key);
                _state = stored ?? RegisterState<TValue>.Default;
                if (_onUpdateState is { } func)
                {
                    await func(_state);
                }

                if (_log.IsEnabled(LogLevel.Trace))
                {
                    _log.LogTrace("Initialized with register state {State}", _state);
                }
            }
        }

        public interface ITestAccessor
        {
            Task EnsureStateLoaded();

            RegisterState<TValue> VolatileState { get; set; }
        }
    }
}