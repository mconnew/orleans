using System;

namespace Orleans.Runtime
{
    public readonly struct GenericGrainType : IEquatable<GenericGrainType>
    {
        private GenericGrainType(GrainType grainType)
        {
            GrainType = grainType;
        }

        public GrainType GrainType { get; }

        public bool IsConstructed => TypeConverterExtensions.IsConstructed(this.GrainType.ToStringUtf8());

        public static bool TryParse(GrainType grainType, out GenericGrainType result)
        {
            if (TypeConverterExtensions.IsGenericType(grainType.ToStringUtf8()))
            {
                result = new GenericGrainType(grainType);
                return true;
            }

            result = default;
            return false;
        }

        public GenericGrainType GetUnconstructedGrainType()
        {
            var str = this.GrainType.ToStringUtf8();
            var generic = TypeConverterExtensions.GetDeconstructed(str);
            return new GenericGrainType(GrainType.Create(generic));
        }

        public GenericGrainType Construct(TypeConverter formatter, params Type[] typeArguments)
        {
            var constructed = formatter.GetConstructed(this.GrainType.ToStringUtf8(), typeArguments);
            return new GenericGrainType(GrainType.Create(constructed));
        }

        public Type[] GetArguments(TypeConverter formatter) => formatter.GetArguments(this.GrainType.ToStringUtf8());

        public override string ToString() => this.GrainType.ToString();

        public bool Equals(GenericGrainType other) => this.GrainType.Equals(other.GrainType);

        public override bool Equals(object obj) => obj is GenericGrainType other && this.Equals(other);

        public override int GetHashCode() => this.GrainType.GetHashCode();
    }
}
