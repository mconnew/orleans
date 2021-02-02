using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.GrainDirectory;

namespace Orleans.Runtime
{
    internal interface ILocalClientDirectory
    {
        bool TryLocalLookup(GrainId grainId, out List<ActivationAddress> addresses);
        ValueTask<List<ActivationAddress>> Lookup(GrainId grainId);
    }
}
