using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Runtime;

namespace Orleans.GrainDirectory.AzureStorage
{
    public class AzureTableGrainDirectory : IGrainDirectory, ILifecycleParticipant<ISiloLifecycle>
    {
        private readonly AzureTableDataManager<GrainDirectoryEntity> tableDataManager;
        private readonly string clusterId;

        private class GrainDirectoryEntity : TableEntity
        {
            public string SiloAddress { get; set; }

            public ActivationAddress ToGrainAddress()
            {
                return new ActivationAddress(
                    grainId: GrainId.Parse(HttpUtility.UrlDecode(this.RowKey, Encoding.UTF8)),
                    siloAddress: Orleans.Runtime.SiloAddress.FromParsableString(this.SiloAddress),
                    eTag: this.ETag);
            }

            public static GrainDirectoryEntity FromGrainAddress(string clusterId, ActivationAddress address)
            {
                return new GrainDirectoryEntity
                {
                    PartitionKey = clusterId,
                    RowKey = HttpUtility.UrlEncode(address.Grain.ToString(), Encoding.UTF8),
                    SiloAddress = address.Silo.ToParsableString(),
                    ETag = address.ETag,
                };
            }
        }

        public AzureTableGrainDirectory(
            AzureTableGrainDirectoryOptions directoryOptions,
            IOptions<ClusterOptions> clusterOptions,
            ILoggerFactory loggerFactory)
        {
            this.tableDataManager = new AzureTableDataManager<GrainDirectoryEntity>(
                directoryOptions,
                loggerFactory.CreateLogger<AzureTableDataManager<GrainDirectoryEntity>>());
            this.clusterId = clusterOptions.Value.ClusterId;
        }

        public async Task<ActivationAddress> Lookup(GrainId grainId)
        {
            var result = await this.tableDataManager.ReadSingleTableEntryAsync(this.clusterId, HttpUtility.UrlEncode(grainId.ToString(), Encoding.UTF8));

            if (result == null)
                return null;

            return result.Item1.ToGrainAddress();
        }

        public async Task<ActivationAddress> Register(ActivationAddress address)
        {
            var entry = GrainDirectoryEntity.FromGrainAddress(this.clusterId, address);
            var result = await this.tableDataManager.InsertTableEntryAsync(entry);
            // Possible race condition?
            return result.isSuccess ? address : await Lookup(address.Grain);
        }

        public async Task Unregister(ActivationAddress address)
        {
            var result = await this.tableDataManager.ReadSingleTableEntryAsync(this.clusterId, HttpUtility.UrlEncode(address.Grain.ToString(), Encoding.UTF8));

            // No entry found
            if (result == null)
            {
                return;
            }

            // Check if the entry in storage match the one we were asked to delete
            var entity = result.Item1;
            if (string.Equals(entity.ETag, address.ETag))
            {
                await this.tableDataManager.DeleteTableEntryAsync(GrainDirectoryEntity.FromGrainAddress(this.clusterId, address), entity.ETag);
            }
        }

        public async Task UnregisterMany(List<ActivationAddress> addresses)
        {
            if (addresses.Count <= this.tableDataManager.StoragePolicyOptions.MaxBulkUpdateRows)
            {
                await UnregisterManyBlock(addresses);
            }
            else
            {
                var tasks = new List<Task>();
                foreach (var subList in addresses.BatchIEnumerable(this.tableDataManager.StoragePolicyOptions.MaxBulkUpdateRows))
                {
                    tasks.Add(UnregisterManyBlock(subList));
                }
                await Task.WhenAll(tasks);
            }
        }

        public Task UnregisterSilos(List<SiloAddress> siloAddresses)
        {
            // Too costly to implement using Azure Table
            return Task.CompletedTask;
        }

        private async Task UnregisterManyBlock(List<ActivationAddress> addresses)
        {
            var pkFilter = TableQuery.GenerateFilterCondition(nameof(GrainDirectoryEntity.PartitionKey), QueryComparisons.Equal, this.clusterId);
            string rkFilter = TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition(nameof(GrainDirectoryEntity.RowKey), QueryComparisons.Equal, HttpUtility.UrlEncode(addresses[0].Grain.ToString(), Encoding.UTF8)),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition(nameof(GrainDirectoryEntity.ETag), QueryComparisons.Equal, addresses[0].ETag)
                    );

            foreach (var addr in addresses.Skip(1))
            {
                var tmp = TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition(nameof(GrainDirectoryEntity.RowKey), QueryComparisons.Equal, HttpUtility.UrlEncode(addr.Grain.ToString(), Encoding.UTF8)),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition(nameof(GrainDirectoryEntity.ETag), QueryComparisons.Equal, addr.ETag)
                    );
                rkFilter = TableQuery.CombineFilters(rkFilter, TableOperators.Or, tmp);
            }

            var entities = await this.tableDataManager.ReadTableEntriesAndEtagsAsync(TableQuery.CombineFilters(pkFilter, TableOperators.And, rkFilter));
            await this.tableDataManager.DeleteTableEntriesAsync(entities.Select(e => Tuple.Create(e.Item1, e.Item2)).ToList());
        }

        // Called by lifecycle, should not be called explicitely, except for tests
        public async Task InitializeIfNeeded(CancellationToken ct = default)
        {
            await this.tableDataManager.InitTableAsync();
        }

        public void Participate(ISiloLifecycle lifecycle)
        {
            lifecycle.Subscribe(nameof(AzureTableGrainDirectory), ServiceLifecycleStage.RuntimeInitialize, InitializeIfNeeded);
        }
    }
}
