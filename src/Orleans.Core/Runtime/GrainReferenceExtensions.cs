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
            return String.Format("{0}", grainRef.GrainId.ToString());
        }
    }
}
