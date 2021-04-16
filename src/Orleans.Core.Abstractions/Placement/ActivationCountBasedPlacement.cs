using System;

namespace Orleans.Runtime
{
    [Serializable]
    [Orleans.GenerateSerializer]
    public class ActivationCountBasedPlacement : PlacementStrategy
    {
        internal static ActivationCountBasedPlacement Singleton { get; } = new ActivationCountBasedPlacement();
    }
}
