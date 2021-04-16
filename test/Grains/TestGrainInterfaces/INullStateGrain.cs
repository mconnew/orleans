using Orleans;
using System.Threading.Tasks;

namespace UnitTests.GrainInterfaces
{
    [Orleans.GenerateSerializer]
    public class NullableState
    {
        [Orleans.Id(0)]
        public string Name { get; set; }
    }

    public interface INullStateGrain : IGrainWithIntegerKey
    {
        Task SetStateAndDeactivate(NullableState state);
        Task<NullableState> GetState();
    }
}