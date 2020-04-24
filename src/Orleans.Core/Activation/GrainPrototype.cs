using System;
using System.Collections.Immutable;
using Orleans.GrainDirectory;

namespace Orleans.Runtime
{
    public class GrainPrototype
    {
        private readonly object featuresLock = new object();
        private ImmutableDictionary<Type, object> features = ImmutableDictionary<Type, object>.Empty;

        public GrainPrototype(GrainType grainType)
        {
            this.GrainType = grainType;
        }

        public GrainType GrainType { get; }

        internal IGrainLocator GrainLocator { get; private set; }

        internal PlacementStrategy PlacementStrategy { get; private set; }

        public TimeSpan? ActivationCollectionLimit { get; private set; }

        public IGrainRuntime GrainRuntime { get; private set; }

        public void Set<T>(T feature) where T : class
        {
            if (typeof(T) == typeof(IGrainLocator))
            {
                this.GrainLocator = (IGrainLocator)feature;
            }
            else if (typeof(T) == typeof(IGrainRuntime))
            {
                this.GrainRuntime = (IGrainRuntime)feature;
            }
            else if (typeof(T) == typeof(PlacementStrategy))
            {
                this.PlacementStrategy = (PlacementStrategy)(object)feature;
            }
            else
            {
                lock (featuresLock)
                {
                    features = features.SetItem(typeof(T), feature);
                }
            }
        }

        public bool TryGet<T>(out T feature) where T : class
        {
            if (typeof(T) == typeof(IGrainLocator))
            {
                feature = (T)this.GrainLocator;
                return true;
            }
            else if (typeof(T) == typeof(IGrainRuntime))
            {
                feature = (T)this.GrainRuntime;
                return true;
            }
            else if (typeof(T) == typeof(PlacementStrategy))
            {
                feature = (T)(object)this.PlacementStrategy;
                return true;
            }
            else
            {
                var result = features.TryGetValue(typeof(T), out var featureObj);
                feature = (T)featureObj;
                return result;
            }
        }
    }
}
