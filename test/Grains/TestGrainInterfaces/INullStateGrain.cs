using Orleans;
using System.Threading.Tasks;

namespace UnitTests.GrainInterfaces
{
    [Hagar.GenerateSerializer]
    public class NullableState
    {
        [Hagar.Id(0)]
        public string Name { get; set; }
    }

    public interface INullStateGrain : IGrainWithIntegerKey
    {
        Task SetStateAndDeactivate(NullableState state);
        Task<NullableState> GetState();
    }
}