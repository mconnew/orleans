using System;

namespace Orleans.Versions.Compatibility
{
    [Serializable]
    [Hagar.GenerateSerializer]
    public class AllVersionsCompatible : CompatibilityStrategy
    {
        public static AllVersionsCompatible Singleton { get; } = new AllVersionsCompatible();
    }
}
