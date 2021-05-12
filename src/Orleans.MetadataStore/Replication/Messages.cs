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

    [GenerateSerializer]
    public enum PrepareStatus : byte
    {
        Unknown = 0,
        Conflict = 1,
        ConfigConflict = 2,
        Success = 3
    }

    [GenerateSerializer]
    public struct PackedPrepareResponse<TValue>
    {
        [Id(0)]
        public byte Status;

        [Id(1)]
        public Ballot Ballot;

        [Id(2)]
        public TValue Value;

        public static PackedPrepareResponse<TValue> Success(Ballot accepted, TValue value) => new()
        {
            Status = (byte)PrepareStatus.Success,
            Ballot = accepted,
            Value = value,
        };

        public static PackedPrepareResponse<TValue> Conflict(Ballot conflicting) => new() 
        {
            Status = (byte)PrepareStatus.Conflict,
            Ballot = conflicting,
        };

        public static PackedPrepareResponse<TValue> ConfigConflict(Ballot conflicting) => new()
        {
            Status = (byte)PrepareStatus.ConfigConflict,
            Ballot = conflicting,
        };

        public void Deconstruct(out PrepareStatus status, out Ballot accepted, out TValue value)
        {
            status = (PrepareStatus)Status;
            accepted = Ballot;
            value = Value;
        }

        public void Deconstruct(out PrepareStatus status, out Ballot conflict)
        {
            status = (PrepareStatus)Status;
            conflict = Ballot;
        }
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
            Conflicting = conflicting;
        }

        [Id(0)]
        public Ballot Conflicting { get; }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(PrepareConflict)}({nameof(Conflicting)}: {Conflicting})";
    }

    [Immutable]
    [Serializable]
    [GenerateSerializer]
    public class PrepareConfigConflict : PrepareResponse
    {
        public PrepareConfigConflict(Ballot conflicting)
        {
            Conflicting = conflicting;
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

    [GenerateSerializer]
    public enum AcceptStatus : byte
    {
        Unknown = 0,
        Conflict = 1,
        ConfigConflict = 2,
        Success = 3
    }

    [GenerateSerializer]
    public struct PackedAcceptResponse
    {
        [Id(0)]
        public byte Status;

        [Id(1)]
        public Ballot Ballot;

        public static PackedAcceptResponse Success() => new()
        {
            Status = (byte)AcceptStatus.Success,
        };

        public static PackedAcceptResponse Conflict(Ballot conflicting) => new() 
        {
            Status = (byte)AcceptStatus.Conflict,
            Ballot = conflicting,
        };

        public static PackedAcceptResponse ConfigConflict(Ballot conflicting) => new()
        {
            Status = (byte)AcceptStatus.ConfigConflict,
            Ballot = conflicting,
        };

        public void Deconstruct(out AcceptStatus status, out Ballot conflict)
        {
            status = (AcceptStatus)Status;
            conflict = Ballot;
        }

        public void Deconstruct(out AcceptStatus status)
        {
            status = (AcceptStatus)Status;
        }
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
