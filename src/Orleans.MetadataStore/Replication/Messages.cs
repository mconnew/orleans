namespace Orleans.MetadataStore
{
    public static class PrepareResponse
    {
        public static PrepareResponse<TValue> Success<TValue>(Ballot accepted, TValue value) => PrepareResponse<TValue>.Success(accepted, value);
        public static PrepareResponse<TValue> Conflict<TValue>(Ballot conflicting) => PrepareResponse<TValue>.Conflict(conflicting);
        public static PrepareResponse<TValue> ConfigConflict<TValue>(Ballot conflicting) => PrepareResponse<TValue>.ConfigConflict(conflicting);
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
    public struct PrepareResponse<TValue>
    {
        [Id(0)]
        public byte _status;

        public PrepareStatus Status => (PrepareStatus)_status;

        [Id(1)]
        public Ballot Ballot;

        [Id(2)]
        public TValue Value;

        public static PrepareResponse<TValue> Success(Ballot accepted, TValue value) => new()
        {
            _status = (byte)PrepareStatus.Success,
            Ballot = accepted,
            Value = value,
        };

        public static PrepareResponse<TValue> Conflict(Ballot conflicting) => new() 
        {
            _status = (byte)PrepareStatus.Conflict,
            Ballot = conflicting,
        };

        public static PrepareResponse<TValue> ConfigConflict(Ballot conflicting) => new()
        {
            _status = (byte)PrepareStatus.ConfigConflict,
            Ballot = conflicting,
        };

        public void Deconstruct(out PrepareStatus status, out Ballot accepted, out TValue value)
        {
            status = Status;
            accepted = Ballot;
            value = Value;
        }

        public void Deconstruct(out PrepareStatus status, out Ballot conflict)
        {
            status = Status;
            conflict = Ballot;
        }
    }

    [GenerateSerializer]
    public enum AcceptStatus : byte
    {
        Unknown = 0,
        Conflict = 1,
        ConfigConflict = 2,
        Success = 3
    }

    [Immutable]
    [GenerateSerializer]
    public struct AcceptResponse
    {
        [Id(0)]
        public byte _status;

        public AcceptStatus Status => (AcceptStatus)_status;

        [Id(1)]
        public Ballot Ballot;

        public static AcceptResponse Success() => new()
        {
            _status = (byte)AcceptStatus.Success,
        };

        public static AcceptResponse Conflict(Ballot conflicting) => new() 
        {
            _status = (byte)AcceptStatus.Conflict,
            Ballot = conflicting,
        };

        public static AcceptResponse ConfigConflict(Ballot conflicting) => new()
        {
            _status = (byte)AcceptStatus.ConfigConflict,
            Ballot = conflicting,
        };

        public void Deconstruct(out AcceptStatus status, out Ballot conflict)
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
