using System;

namespace Orleans.Runtime
{
    public readonly struct GenericGrainInterfaceId
    {
        private GenericGrainInterfaceId(GrainInterfaceId value)
        {
            Value = value;
        }

        public GrainInterfaceId Value { get; }

        public bool IsConstructed => TypeConverterExtensions.IsConstructed(this.Value.ToStringUtf8());

        public static bool TryParse(GrainInterfaceId grainType, out GenericGrainInterfaceId result)
        {
            if (!grainType.IsDefault && TypeConverterExtensions.IsGenericType(grainType.ToStringUtf8()))
            {
                result = new GenericGrainInterfaceId(grainType);
                return true;
            }

            result = default;
            return false;
        }

        public GenericGrainInterfaceId GetGenericGrainType()
        {
            var str = this.Value.ToStringUtf8();
            var generic = TypeConverterExtensions.GetDeconstructed(str);
            return new GenericGrainInterfaceId(GrainInterfaceId.Create(generic));
        }

        public GenericGrainInterfaceId Construct(TypeConverter formatter, params Type[] typeArguments)
        {
            var constructed = formatter.GetConstructed(this.Value.ToStringUtf8(), typeArguments);
            return new GenericGrainInterfaceId(GrainInterfaceId.Create(constructed));
        }

        public Type[] GetArguments(TypeConverter formatter) => formatter.GetArguments(this.Value.ToStringUtf8());

        public string GetArgumentsString() => TypeConverterExtensions.GetArgumentsString(this.Value.ToStringUtf8());

        public override string ToString() => this.Value.ToString();
    }
}
