using System;

namespace Orleans.Runtime
{
    [Serializable]
    [Hagar.GenerateSerializer]
    public class PreferLocalPlacement : PlacementStrategy
    {
        internal static PreferLocalPlacement Singleton { get; } = new PreferLocalPlacement();
    }
}
