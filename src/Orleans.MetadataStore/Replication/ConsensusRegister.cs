using System;

namespace Orleans.MetadataStore
{
    public class ConsensusRegister<TValue> : IVolatileAcceptor<TValue>, ConsensusRegister<TValue>.ITestAccessor
    {
        private readonly Func<Ballot> _getParentBallot;
        private readonly Action<TValue> _onUpdateState;
        private Ballot _promised;
        private Ballot _accepted;
        private TValue _value;
        
        public ConsensusRegister(
            Func<Ballot> getParentBallot,
            Action<TValue> onUpdateState)
        {
            _getParentBallot = getParentBallot;
            _onUpdateState = onUpdateState;
        }

        Ballot ITestAccessor.Promised { get => _promised; set => _promised = value; }
        Ballot ITestAccessor.Accepted { get => _accepted; set => _accepted = value; }
        TValue ITestAccessor.VolatileState { get => _value; set => _value = value; }

        public PrepareResponse<TValue> Prepare(Ballot proposerParentBallot, Ballot ballot)
        {
            PrepareResponse<TValue> result;
            var parentBallot = _getParentBallot();
            if (parentBallot > proposerParentBallot)
            {
                // If the proposer is using a cluster configuration version which is lower than the highest
                // cluster configuration version observed by this node, then the Prepare is rejected.
                result = PrepareResponse<TValue>.ConfigConflict(parentBallot);
            }
            else if (_promised > ballot)
            {
                // If a Prepare with a higher ballot has already been encountered, reject this.
                result = PrepareResponse<TValue>.Conflict(_promised);
            }
            else if (_accepted > ballot)
            {
                // If an Accept with a higher ballot has already been encountered, reject this.
                result = PrepareResponse<TValue>.Conflict(_accepted);
            }
            else
            {
                // Record a tentative promise to accept this proposer's value.
                _promised = ballot;
                result = PrepareResponse<TValue>.Success(_accepted, _value);
            }

            return result;
        }

        public AcceptResponse Accept(Ballot proposerParentBallot, Ballot ballot, TValue value)
        {
            AcceptResponse result;
            var parentBallot = _getParentBallot();
            if (parentBallot > proposerParentBallot)
            {
                // If the proposer is using a cluster configuration version which is lower than the highest
                // cluster configuration version observed by this node, then the Accept is rejected.
                result = AcceptResponse.ConfigConflict(parentBallot);
            }
            else if (_promised > ballot)
            {
                // If a Prepare with a higher ballot has already been encountered, reject this.
                result = AcceptResponse.Conflict(_promised);
            }
            else if (_accepted > ballot)
            {
                // If an Accept with a higher ballot has already been encountered, reject this.
                result = AcceptResponse.Conflict(_accepted);
            }
            else
            {
                // Record the new state.
                _promised = ballot;
                _accepted = ballot;
                _value = value;
                _onUpdateState?.Invoke(_value);
                result = AcceptResponse.Success();
            }

            return result;
        }

        internal void ForceState(TValue newState)
        {
            _accepted = Ballot.Zero;
            _promised = Ballot.Zero;
            _value = newState;
        }

        public interface ITestAccessor
        {
            Ballot Promised { get; set; }
            Ballot Accepted { get; set; }
            TValue VolatileState { get; set; }
        }
    }
}