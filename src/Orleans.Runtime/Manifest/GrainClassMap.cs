using System;
using System.Collections.Immutable;
using Orleans.Runtime;

namespace Orleans.Metadata
{
    public class GrainClassMap
    {
        public GrainClassMap(ImmutableDictionary<GrainType, Type> classes) => this.AvailableTypes = classes;
        public ImmutableDictionary<GrainType, Type> AvailableTypes { get; }
    }
}
