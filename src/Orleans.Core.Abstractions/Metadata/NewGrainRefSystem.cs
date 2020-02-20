using System;
using System.Collections.Generic;
using System.Text;
using Orleans.CodeGeneration;
using Orleans.Runtime;

namespace Orleans.Metadata
{
    namespace NewGrainRefSystem
    {
        public interface IGrainReference
        {
            GrainId GrainId { get; }

            Type InterfaceType { get; }

            // Eg, to set TTL, InvokeMethodOptions.Unordered, InvokeMethodOptions.OneWay?, SystemTargetSilo?, Category?
            // How does ResponseTimeout get propagated?
            //void PrepareMessage(object /*IMessage*/ message);
        }

        public class ImrGrainReferencePrototype
        {
            public ImrGrainReferencePrototype(GrainType grainType, Type interfaceType, IGrainReferenceRuntime grainRuntime)
            {
                this.GrainType = grainType;
                this.InterfaceType = interfaceType;
                this.Runtime = grainRuntime;
            }

            // Everything shared by a grain for given type
            public GrainType GrainType { get; }

            public Type InterfaceType { get; }

            public IGrainReferenceRuntime Runtime { get; }

            public InvokeMethodOptions InvokeMethodOptions { get; set; }
        }
    }
}
