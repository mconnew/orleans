using System;
using System.Collections.Immutable;
using Orleans.Runtime;

namespace Orleans.Metadata
{
    public class GrainClassMap
    {
        private readonly TypeConverter _typeConverter;
        private readonly ImmutableDictionary<GrainType, Type> _types;

        public GrainClassMap(TypeConverter typeConverter, ImmutableDictionary<GrainType, Type> classes)
        {
            _typeConverter = typeConverter;
            _types = classes;
        }

        public bool TryGetGrainClass(GrainType grainType, out Type grainClass)
        {
            GrainType lookupType;
            Type[] args;
            if (GenericGrainType.TryParse(grainType, out var genericId))
            {
                lookupType = genericId.GetUnconstructedGrainType().GrainType;
                args = genericId.GetArguments(_typeConverter);
            }
            else
            {
                lookupType = grainType;
                args = default;
            }

            if (!_types.TryGetValue(lookupType, out grainClass))
            {
                return false;
            }

            if (args is object)
            {
                grainClass = grainClass.MakeGenericType(args);
            }

            return true;
        }
    }
}
