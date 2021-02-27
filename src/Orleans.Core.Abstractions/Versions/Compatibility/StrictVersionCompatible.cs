using System;

namespace Orleans.Versions.Compatibility
{
    [Serializable]
    [Hagar.GenerateSerializer]
    public class StrictVersionCompatible : CompatibilityStrategy
    {
        public static StrictVersionCompatible Singleton { get; } = new StrictVersionCompatible();
    }
}