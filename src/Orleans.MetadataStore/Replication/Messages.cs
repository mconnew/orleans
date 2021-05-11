using System;
using Orleans.Concurrency;

namespace Orleans.MetadataStore
{
    [Serializable]
    [GenerateSerializer]
    public abstract class PrepareResponse
    {
        public static PrepareSuccess<TValue> Success<TValue>(Ballot accepted, TValue value) => new PrepareSuccess<TValue>(accepted, value);
        public static PrepareConflict Conflict(Ballot conflicting) => new PrepareConflict(conflicting);
        public static PrepareConfigConflict ConfigConflict(Ballot conflicting) => new PrepareConfigConflict(conflicting);
    }

    [Serializable]
    [GenerateSerializer]
    public class PrepareSuccess<TValue> : PrepareResponse
    {
        public PrepareSuccess(Ballot accepted, TValue value)
        {
            Accepted = accepted;
            Value = value;
        }

        [Id(0)]
        public TValue Value { get; }

        [Id(1)]
        public Ballot Accepted { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{nameof(PrepareSuccess<TValue>)}({nameof(Accepted)}: {Accepted}, {nameof(Value)}: {Value})";
        }
    }

    [Immutable]
    [Serializable]
    [GenerateSerializer]
    public class PrepareConflict : PrepareResponse
    {
        public PrepareConflict(Ballot conflicting)
        {
            this.Conflicting = conflicting;
        }

        [Id(0)]
        public Ballot Conflicting { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{nameof(PrepareConflict)}({nameof(Conflicting)}: {Conflicting})";
        }
    }

    [Immutable]
    [Serializable]
    [GenerateSerializer]
    public class PrepareConfigConflict : PrepareResponse
    {
        public PrepareConfigConflict(Ballot conflicting)
        {
            this.Conflicting = conflicting;
        }

        [Id(0)]
        public Ballot Conflicting { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{nameof(PrepareConfigConflict)}({nameof(Conflicting)}: {Conflicting})";
        }
    }

    [Serializable]
    [GenerateSerializer]
    public abstract class AcceptResponse
    {
        public static AcceptSuccess Success() => AcceptSuccess.Instance;

        public static AcceptConflict Conflict(Ballot conflicting) => new AcceptConflict(conflicting);

        public static AcceptConfigConflict ConfigConflict(Ballot conflicting) => new AcceptConfigConflict(conflicting);
    }

    [Immutable]
    [Serializable]
    [GenerateSerializer]
    public class AcceptSuccess : AcceptResponse
    {
        public static AcceptSuccess Instance { get; } = new AcceptSuccess();

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{nameof(AcceptSuccess)}()";
        }
    }

    [Immutable]
    [Serializable]
    [GenerateSerializer]
    public class AcceptConflict : AcceptResponse
    {
        public AcceptConflict(Ballot conflicting)
        {
            Conflicting = conflicting;
        }

        [Id(0)]
        public Ballot Conflicting { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{nameof(AcceptConflict)}({nameof(Conflicting)}: {Conflicting})";
        }
    }

    [Immutable]
    [Serializable]
    [GenerateSerializer]
    public class AcceptConfigConflict : AcceptResponse
    {
        public AcceptConfigConflict(Ballot conflicting)
        {
            Conflicting = conflicting;
        }

        [Id(0)]
        public Ballot Conflicting { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{nameof(AcceptConflict)}({nameof(Conflicting)}: {Conflicting})";
        }
    }
}
