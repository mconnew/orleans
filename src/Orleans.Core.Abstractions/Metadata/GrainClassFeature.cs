using System.Collections.Generic;

namespace Orleans.Metadata
{
    /// <summary>
    /// Contains grain class descriptions.
    /// </summary>
    public class GrainClassFeature
    {
        /// <summary>
        /// Gets a collection of grain classes.
        /// </summary>
        public IList<GrainClassTypeDescriptor> Classes { get; } = new List<GrainClassTypeDescriptor>();
    }
}
