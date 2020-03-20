using System;

namespace Orleans.Runtime
{
    public static class GrainReferenceExtensions
    {
        /// <summary>
        /// Key string for grain references as unique as ToKeyString, but shorter and parseable.  Intended for use where
        /// uniqueness and brevity are important.
        /// </summary>
        public static string ToShortKeyString(this GrainReference grainRef)
        {
//TODO: Implement in backwards compatible manner (check if GrainId is LegacyGrainId and ToString that?)
            if (grainRef.IsObserverReference)
            {
                return String.Format("{0}_{1}", grainRef.GrainId.ToString(), grainRef.ObserverId.ToParsableString());
            }
            /*
            if (grainRef.IsSystemTarget)
            {
                return String.Format("{0}_{1}", grainRef.GrainId.ToString(), grainRef.SystemTargetSilo.ToParsableString());
            }
            */
            if (grainRef.HasGenericArgument)
            {
                return String.Format("{0}_{1}", grainRef.GrainId.ToString(), grainRef.GenericArguments);
            }
            return String.Format("{0}", grainRef.GrainId.ToString());
        }
    }
}
