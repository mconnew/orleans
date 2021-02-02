using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.GrainDirectory;

namespace Orleans.Runtime.GrainDirectory
{
    internal static class GrainDirectoryCacheFactory
    {
        internal static IGrainDirectoryCache CreateGrainDirectoryCache(GrainDirectoryOptions options)
        {
            if (options.CacheSize <= 0)
                return new NullGrainDirectoryCache();
            
            switch (options.CachingStrategy)
            {
                case GrainDirectoryOptions.CachingStrategyType.None:
                    return new NullGrainDirectoryCache();
                case GrainDirectoryOptions.CachingStrategyType.LRU:
                    return new LRUBasedGrainDirectoryCache(options.CacheSize, options.MaximumCacheTTL);
                default:
                    return new AdaptiveGrainDirectoryCache(options.InitialCacheTTL, options.MaximumCacheTTL, options.CacheTTLExtensionFactor, options.CacheSize);
            }
        }

        internal static AdaptiveDirectoryCacheMaintainer CreateGrainDirectoryCacheMaintainer(
            LocalGrainDirectory router,
            IGrainDirectoryCache cache,
            IInternalGrainFactory grainFactory,
            ILoggerFactory loggerFactory)
        {
            var adaptiveCache = cache as AdaptiveGrainDirectoryCache;
            return adaptiveCache != null
                ? new AdaptiveDirectoryCacheMaintainer(router, adaptiveCache, grainFactory, loggerFactory)
                : null;
        }
    }

    internal class NullGrainDirectoryCache : IGrainDirectoryCache
    {
        private static readonly List<ActivationAddress> EmptyList = new List<ActivationAddress>();

        public List<ActivationAddress> Entries => EmptyList;

        public void AddOrUpdate(GrainId key, ActivationAddress value) { } 

        public void Clear() { }

        public bool LookUp(GrainId key, out ActivationAddress result)
        {
            result = default;
            return false;
        }

        public bool Remove(ActivationAddress key) => false;
    }
}

