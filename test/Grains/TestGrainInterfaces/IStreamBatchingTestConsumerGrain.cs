
using System.Threading.Tasks;
using Orleans;

namespace UnitTests.GrainInterfaces
{
    public static class StreamBatchingTestConst
    {
        public const string ProviderName = "StreamBatchingTest";
        public const string BatchingNameSpace = "batching";
        public const string NonBatchingNameSpace = "nonbatching";
    }

    [Orleans.GenerateSerializer]
    public class ConsumptionReport
    {
        [Orleans.Id(0)]
        public int Consumed { get; set; }

        [Orleans.Id(1)]
        public int MaxBatchSize { get; set; }
    }

    public interface IStreamBatchingTestConsumerGrain : IGrainWithGuidKey
    {
        Task<ConsumptionReport> GetConsumptionReport();
    }
}
