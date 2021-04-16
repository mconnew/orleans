using System;

namespace Orleans.Versions.Selector
{
    [Serializable]
    [Orleans.GenerateSerializer]
    public class MinimumVersion : VersionSelectorStrategy
    {
        public static MinimumVersion Singleton { get; } = new MinimumVersion();
    }
}