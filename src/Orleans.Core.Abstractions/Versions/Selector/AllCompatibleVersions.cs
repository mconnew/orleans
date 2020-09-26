using System;

namespace Orleans.Versions.Selector
{
    [Serializable]
    [Hagar.GenerateSerializer]
    public class AllCompatibleVersions : VersionSelectorStrategy
    {
        public static AllCompatibleVersions Singleton { get; } = new AllCompatibleVersions();
    }
}