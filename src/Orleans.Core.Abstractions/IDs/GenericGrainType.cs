using System;
using System.Runtime.CompilerServices;

namespace Orleans.Runtime
{
    /// <summary>
    /// Methods for creating and parsing parameterised <see cref="GrainType"/>s.
    /// </summary>
    public static class GenericGrainType
    {
        public static readonly char GenericParameterIndicator = '`';
        public static readonly string GenericParameterIndicatorString = GenericParameterIndicator.ToString();

        public static GrainType Create(GrainType baseType, string genericArgs)
        {
            var typeString = baseType.ToStringUtf8();
            if (IsGenericGrainType(typeString))
            {
                ThrowInvalidGrainType(baseType);
            }

            return GrainType.Create(typeString + GenericParameterIndicator + genericArgs);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidGrainType(GrainType grainType) => throw new ArgumentException($"GrainType {grainType} is already a generic grain type.");

        public static bool IsGenericGrainType(this GrainType grainType) => IsGenericGrainType(grainType.ToStringUtf8());

        public static bool IsGenericGrainType(string grainTypeString) => grainTypeString.Contains(GenericParameterIndicatorString);

        public static string GetGenericArgumentsOrDefault(this GrainType grainType) => grainType.TryGetGenericArguments(out var result) ? result : null;

        public static bool TryGetGenericArguments(this GrainType grainType, out string genericArgs)
        {
            var typeString = grainType.ToStringUtf8();
            if (typeString.IndexOf(GenericParameterIndicator) is int index && index >= 0)
            {
                genericArgs = typeString.Substring(index + 1);
                return true;
            }

            genericArgs = default;
            return false;
        }
    }
}
