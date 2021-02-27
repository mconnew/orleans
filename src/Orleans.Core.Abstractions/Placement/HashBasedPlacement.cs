using System;

namespace Orleans.Runtime
{
    [Serializable]
    [Hagar.GenerateSerializer]
    public class HashBasedPlacement : PlacementStrategy
    {
        internal static HashBasedPlacement Singleton { get; } = new HashBasedPlacement();
    }
}
