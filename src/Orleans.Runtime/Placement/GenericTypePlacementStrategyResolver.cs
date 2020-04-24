using System;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Metadata;

namespace Orleans.Runtime.Placement
{
    internal class GenericTypePlacementStrategyResolver : IPlacementStrategyResolver
    {
        private readonly IServiceProvider _services;
        private PlacementStrategyResolver _resolver;

        public GenericTypePlacementStrategyResolver(IServiceProvider services)
        {
            _services = services;
        }

        public bool TryResolvePlacementStrategy(GrainType grainType, GrainProperties properties, out PlacementStrategy result)
        {
            if (GenericGrainType.TryParse(grainType, out var constructed) && constructed.IsConstructed)
            {
                var generic = constructed.GetGenericGrainType().GrainType;
                var resolver = GetResolver();
                if (resolver.TryGetNonDefaultPlacementStrategy(generic, out result))
                {
                    return true;
                }
            }

            result = default;
            return false;
        }

        private PlacementStrategyResolver GetResolver() => _resolver ??= _services.GetRequiredService<PlacementStrategyResolver>();
    }
}
