using System;
using System.Text;

namespace Orleans.Runtime
{
    [Serializable]
    [GenerateSerializer]
    public class RandomPlacement : PlacementStrategy
    {
        internal static RandomPlacement Singleton { get; } = new RandomPlacement();
    }
}
