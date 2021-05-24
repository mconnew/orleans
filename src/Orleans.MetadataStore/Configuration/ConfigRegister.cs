using System;

namespace Orleans.MetadataStore
{
    public class ConfigRegister<TValue> : ConfigRegister<TValue>.ITestAccessor
    {
        private readonly Action<TValue> _onAcceptState;
        private ConfigBallot _promised;
        private ConfigBallot _accepted;
        private TValue _value;
        
        public ConfigRegister(Action<TValue> onAcceptState)
        {
            _onAcceptState = onAcceptState;
        }

        ConfigBallot ITestAccessor.Promised { get => _promised; set => _promised = value; }
        ConfigBallot ITestAccessor.Accepted { get => _accepted; set => _accepted = value; }
        TValue ITestAccessor.VolatileState { get => _value; set => _value = value; }

        public ConfigPrepareResponse<TValue> Prepare(ConfigBallot ballot)
        {
            ConfigPrepareResponse<TValue> result;
            if (_promised > ballot)
            {
                // If a Prepare with a higher ballot has already been encountered, reject this.
                result = ConfigPrepareResponse<TValue>.Conflict(_promised);
            }
            else if (_accepted > ballot)
            {
                // If an Accept with a higher ballot has already been encountered, reject this.
                result = ConfigPrepareResponse<TValue>.Conflict(_accepted);
            }
            else
            {
                // Record a tentative promise to accept this proposer's value.
                _promised = ballot;
                result = ConfigPrepareResponse<TValue>.Success(_accepted, _value);
            }

            return result;
        }

        public ConfigAcceptResponse Accept(ConfigBallot ballot, TValue value)
        {
            ConfigAcceptResponse result;
            if (_promised > ballot)
            {
                // If a Prepare with a higher ballot has already been encountered, reject this.
                result = ConfigAcceptResponse.Conflict(_promised);
            }
            else if (_accepted > ballot)
            {
                // If an Accept with a higher ballot has already been encountered, reject this.
                result = ConfigAcceptResponse.Conflict(_accepted);
            }
            else
            {
                // Record the new state.
                _promised = ballot;
                _accepted = ballot;
                _value = value;
                _onAcceptState?.Invoke(_value);
                result = ConfigAcceptResponse.Success();
            }

            return result;
        }

        internal void ForceState(TValue newState)
        {
            _accepted = ConfigBallot.Zero;
            _promised = ConfigBallot.Zero;
            _value = newState;
        }

        public interface ITestAccessor
        {
            ConfigBallot Promised { get; set; }
            ConfigBallot Accepted { get; set; }
            TValue VolatileState { get; set; }
        }
    }

    [GenerateSerializer]
    public struct ConfigPrepareResponse<TValue>
    {
        [Id(0)]
        public byte _status;

        public PrepareStatus Status => (PrepareStatus)_status;

        [Id(1)]
        public ConfigBallot Ballot;

        [Id(2)]
        public TValue Value;

        public static ConfigPrepareResponse<TValue> Success(ConfigBallot accepted, TValue value) => new()
        {
            _status = (byte)PrepareStatus.Success,
            Ballot = accepted,
            Value = value,
        };

        public static ConfigPrepareResponse<TValue> Conflict(ConfigBallot conflicting) => new() 
        {
            _status = (byte)PrepareStatus.Conflict,
            Ballot = conflicting,
        };

        public static ConfigPrepareResponse<TValue> ConfigConflict(ConfigBallot conflicting) => new()
        {
            _status = (byte)PrepareStatus.ConfigConflict,
            Ballot = conflicting,
        };

        public void Deconstruct(out PrepareStatus status, out ConfigBallot accepted, out TValue value)
        {
            status = Status;
            accepted = Ballot;
            value = Value;
        }

        public void Deconstruct(out PrepareStatus status, out ConfigBallot conflict)
        {
            status = Status;
            conflict = Ballot;
        }
    }

    [Immutable]
    [GenerateSerializer]
    public struct ConfigAcceptResponse
    {
        [Id(0)]
        public byte _status;

        public AcceptStatus Status => (AcceptStatus)_status;

        [Id(1)]
        public ConfigBallot Ballot;

        public static ConfigAcceptResponse Success() => new()
        {
            _status = (byte)AcceptStatus.Success,
        };

        public static ConfigAcceptResponse Conflict(ConfigBallot conflicting) => new() 
        {
            _status = (byte)AcceptStatus.Conflict,
            Ballot = conflicting,
        };

        public static ConfigAcceptResponse ConfigConflict(ConfigBallot conflicting) => new()
        {
            _status = (byte)AcceptStatus.ConfigConflict,
            Ballot = conflicting,
        };

        public void Deconstruct(out AcceptStatus status, out ConfigBallot conflict)
        {
            status = (AcceptStatus)_status;
            conflict = Ballot;
        }

        public void Deconstruct(out AcceptStatus status)
        {
            status = (AcceptStatus)_status;
        }
    }
}