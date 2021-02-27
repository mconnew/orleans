using System;

namespace Orleans.Versions.Selector
{
    [Serializable]
    [Hagar.GenerateSerializer]
    public class LatestVersion : VersionSelectorStrategy
    {
        public static LatestVersion Singleton { get; } = new LatestVersion();
    }
}