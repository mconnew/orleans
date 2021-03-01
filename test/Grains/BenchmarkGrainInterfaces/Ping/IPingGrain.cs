using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace BenchmarkGrainInterfaces.Ping
{
    public interface IPingGrain : IGrainWithIntegerKey
    {
        Task Run();

        [AlwaysInterleave]
        Task PingPongInterleave(IPingGrain other, int count);

        Task<int> GetSiloPort();
    }
}
