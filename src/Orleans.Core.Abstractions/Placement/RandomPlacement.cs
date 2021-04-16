using System;

namespace Orleans.Runtime
{
    [Serializable]
    [Orleans.GenerateSerializer]
    public class RandomPlacement : PlacementStrategy
    {
        internal static RandomPlacement Singleton { get; } = new RandomPlacement();
    }
}
