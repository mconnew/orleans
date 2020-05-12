using System;
using System.Runtime.CompilerServices;
using System.Text;
using Orleans.GrainReferences;

namespace Orleans.Runtime
{
    public interface IGrainReferenceConverter
    {
        /// <summary>
        /// Creates a grain reference from a storage key string.
        /// </summary>
        /// <param name="key">The key string.</param>
        /// <returns>The newly created grain reference.</returns>
        GrainReference GetGrainFromKeyString(string key);

        string GetKeyString(GrainReference grainReference);
    }

    internal class GrainReferenceConverter : IGrainReferenceConverter
    {
        private readonly GrainReferenceActivator _activator;

        public GrainReferenceConverter(GrainReferenceActivator activator)
        {
            _activator = activator;
        }

        public GrainReference GetGrainFromKeyString(string referenceString)
        {
            var splits = referenceString.Split('_');
            var type = new GrainType(Convert.FromBase64String(splits[0]));
            var key = new IdSpan(Convert.FromBase64String(splits[1]));
            var id = new GrainId(type, key);
            return _activator.CreateReference(id, default);
        }

        string IGrainReferenceConverter.GetKeyString(GrainReference grainReference) => grainReference.ToKeyString();
    }

    public static class ConverterGrainReferenceExtensions
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