using System;

namespace Orleans.MetadataStore
{
    [Immutable]
    [GenerateSerializer]
    public readonly struct Ballot : IComparable<Ballot>
    {
        /// <summary>
        /// The proposal number.
        /// </summary>
        [Id(0)]
        public readonly int Counter;

        /// <summary>
        /// The unique identifier of the proposer.
        /// </summary>
        [Id(1)]
        public readonly int Id;

        public Ballot(int counter, int id)
        {
            Counter = counter;
            Id = id;
        }

        public Ballot Successor() => new(Counter + 1, Id);

        public Ballot AdvanceTo(Ballot other) => new(Math.Max(Counter, other.Counter), Id);

        public static Ballot Zero => default;

        public bool IsZero() => Equals(Zero);

        /// <inheritdoc />
        public override string ToString() => IsZero() ? $"{nameof(Ballot)}(ø)" : $"{nameof(Ballot)}({Counter}.{Id})";

        public bool Equals(Ballot other)
        {
            return Counter == other.Counter && Id == other.Id;
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is Ballot ballot && Equals(ballot);

        /// <inheritdoc />
        public int CompareTo(Ballot other)
        {
            var counterComparison = Counter - other.Counter;
            if (counterComparison != 0)
            {
                return counterComparison;
            }

            return Id.CompareTo(other.Id);
        }

        public static bool operator ==(Ballot left, Ballot right) => left.Equals(right);

        public static bool operator !=(Ballot left, Ballot right) => !left.Equals(right);

        public static bool operator <(Ballot left, Ballot right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(Ballot left, Ballot right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(Ballot left, Ballot right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(Ballot left, Ballot right)
        {
            return left.CompareTo(right) >= 0;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Counter.GetHashCode() * 397) ^ Id.GetHashCode();
            }
        }
    }
}