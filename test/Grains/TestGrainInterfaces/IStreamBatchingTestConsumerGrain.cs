
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

    [Hagar.GenerateSerializer]
    public class ConsumptionReport
    {
        [Hagar.Id(0)]
        public int Consumed { get; set; }

        [Hagar.Id(1)]
        public int MaxBatchSize { get; set; }
    }

    public interface IStreamBatchingTestConsumerGrain : IGrainWithGuidKey
    {
        Task<ConsumptionReport> GetConsumptionReport();
    }
}
