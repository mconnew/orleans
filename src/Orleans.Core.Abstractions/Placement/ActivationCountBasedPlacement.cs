using System;

namespace Orleans.Runtime
{
    [Serializable]
    [Hagar.GenerateSerializer]
    public class ActivationCountBasedPlacement : PlacementStrategy
    {
        internal static ActivationCountBasedPlacement Singleton { get; } = new ActivationCountBasedPlacement();
    }
}
