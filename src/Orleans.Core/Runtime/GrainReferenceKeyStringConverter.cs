using System;
using Orleans.GrainReferences;

namespace Orleans.Runtime
{
    public class GrainReferenceKeyStringConverter
    {
        private readonly GrainReferenceActivator _activator;

        public GrainReferenceKeyStringConverter(GrainReferenceActivator activator)
        {
            _activator = activator;
        }

        public GrainReference FromKeyString(string referenceString)
        {
            var splits = referenceString.Split('_');
            var type = new GrainType(Convert.FromBase64String(splits[0]));
            var key = new IdSpan(Convert.FromBase64String(splits[1]));
            var id = new GrainId(type, key);
            return _activator.CreateReference(id, default);
        }

        public string ToKeyString(GrainReference grainReference) => grainReference.ToKeyString();
    }

    public static class GrainReferenceConverterExtensions
    {
        public static string ToKeyString(this GrainReference grainReference)
        {
            var id = grainReference.GrainId;
            var typeString = Convert.ToBase64String(GrainType.UnsafeGetArray(id.Type));
            var keyString = Convert.ToBase64String(IdSpan.UnsafeGetArray(id.Key));
            return $"{typeString}_{keyString}";
        }
    }
}