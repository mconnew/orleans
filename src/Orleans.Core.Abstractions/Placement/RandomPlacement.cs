using System;

namespace Orleans.Runtime
{
    [Serializable]
    [Hagar.GenerateSerializer]
    public class RandomPlacement : PlacementStrategy
    {
        internal static RandomPlacement Singleton { get; } = new RandomPlacement();
    }
}
