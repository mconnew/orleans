using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Configuration.Internal;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.TestingHost;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.MetadataStore.Tests
{
    [Trait("Category", "MetadataStore")]
    public class MetadataStoreClusteringTests : IClassFixture<MetadataStoreClusteringTests.Fixture>
    {
        private readonly ITestOutputHelper output;
        private readonly Fixture fixture;
        public class Fixture
        {
            public Fixture()
            {
                var builder = new TestClusterBuilder();
                builder.Options.InitialSilosCount = 3;
                builder.AddSiloBuilderConfigurator<SiloConfigurator>();
                builder.AddClientBuilderConfigurator<ClientConfigurator>();
                var testCluster = builder.Build();
                if (testCluster?.Primary == null)
                {
                    testCluster?.Deploy();
                }

                this.HostedCluster = testCluster;
            }

            public TestCluster HostedCluster { get; }

            public IClusterClient Client => this.HostedCluster?.Client;

            public virtual void Dispose() => this.HostedCluster?.StopAllSilos();
        }

        public class SiloConfigurator : ISiloBuilderConfigurator
        {
            public void Configure(ISiloHostBuilder builder)
            {
                builder
                    //.ConfigureLogging(logging => logging.AddDebug())
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<MetadataStoreMembershipTable>();
                        services.AddFromExisting<ILifecycleParticipant<ISiloLifecycle>, MetadataStoreMembershipTable>();
                        services.AddFromExisting<IMembershipTable, MetadataStoreMembershipTable>();
                        services.AddOptions<MetadataStoreClusteringOptions>()
                            .Configure((MetadataStoreClusteringOptions options, ILocalSiloDetails localSiloDetails) =>
                            {
                                options.SeedNodes = new[] { localSiloDetails.SiloAddress };
                            });
                    })
                    .ConfigureLogging(l => l.AddDebug())
                    .AddMetadataStore()
                    .UseMemoryLocalStore()
                    .AddStartupTask<BootstrapCluster>()
                    .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IMetadataStoreGrain).Assembly));
            }
        }

        internal class BootstrapCluster : IStartupTask
        {
            private readonly ILocalSiloDetails localSiloDetails;
            private readonly ConfigurationManager configurationManager;
            private readonly IClusterMembershipService membershipService;
            private readonly ILogger<BootstrapCluster> log;
            private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

            public BootstrapCluster(
                ILocalSiloDetails localSiloDetails,
                ConfigurationManager configurationManager,
                IClusterMembershipService membershipService,
                ILogger<BootstrapCluster> log)
            {
                this.localSiloDetails = localSiloDetails;
                this.configurationManager = configurationManager;
                this.membershipService = membershipService;
                this.log = log;
            }

            public async Task Execute(CancellationToken cancellationToken)
            {
                if (this.configurationManager.AcceptedConfiguration?.Configuration == null)
                {
                    await this.configurationManager.ForceLocalConfiguration(
                        new ReplicaSetConfiguration(
                            stamp: new Ballot(1, this.configurationManager.NodeId),
                            version: 1,
                            nodes: new[] {this.localSiloDetails.SiloAddress},
                            acceptQuorum: 1,
                            prepareQuorum: 1,
                            ranges: default,
                            values: default));
                }

                //await this.configurationManager.TryAddServer(this.localSiloDetails.SiloAddress);
                _ = Task.Run(RunAsync);
            }

            private async Task RunAsync()
            {
                while (!this.cancellation.IsCancellationRequested)
                {
                    try
                    {
                        var previous = default(ClusterMembershipSnapshot);
                        await foreach (var snapshot in this.membershipService.MembershipUpdates.WithCancellation(this.cancellation.Token))
                        {
                            var update = previous is null ? snapshot.AsUpdate() : snapshot.CreateUpdate(previous);
                            foreach (var change in update.Changes)
                            {
                                var (silo, status) = (change.SiloAddress, change.Status);
                                log.LogInformation($"Got silo update: {silo} -> {status}");
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        this.log.LogError(exception, "Exception in RunAsync. Continuing.");
                    }
                }
            }
        }

        public class ClientConfigurator : IClientBuilderConfigurator
        {
            public void Configure(IConfiguration configuration, IClientBuilder builder)
            {
                //builder.ConfigureLogging(logging => logging.AddDebug());
            }
        }

        public MetadataStoreClusteringTests(ITestOutputHelper output, Fixture fixture)
        {
            this.output = output;
            this.fixture = fixture;
        }

        [Fact, Trait("Category", "BVT")]
        public async Task MetadataStore_Membership_Basic()
        {
            var log = new XunitLogger(this.output, $"Client-{1}");
            var grain = this.fixture.Client.GetGrain<IMetadataStoreGrain>(Guid.NewGuid());
            var result = await grain.TryUpdate("testKey", new MyVersionedData {Value = "initial", Version = 1});
            log.LogInformation($"Wrote data and got answer: {result}");

            await Task.Delay(5000);
            var configResult = await grain.Get<ReplicaSetConfiguration>("MDS.Config");
            log.LogInformation($"Read config and got answer: {configResult}");

            var readResult = await grain.Get<MyVersionedData>("testKey");
            log.LogInformation($"Read data and got answer: {readResult}");

            var data = readResult.Value;
            for (var i = 0; i < 1000; i++)
            {
                data.Version++;
                //data.Value = data.Value + ", " + i;
                result = await grain.TryUpdate("testKey", data);
                //log.LogInformation($"Wrote data and got answer: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
                data = result.Value;
            }
        }
    }
}
