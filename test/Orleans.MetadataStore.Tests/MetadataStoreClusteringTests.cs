using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Orleans.Configuration;
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
        private SiloAddress[] seedNodes = new SiloAddress[0];

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


        public class SiloConfigurator : ISiloConfigurator
        {
            public void Configure(ISiloBuilder builder)
            {
                builder
                    .ConfigureLogging(logging => logging.AddDebug())
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<MetadataStoreMembershipTable>();
                        services.AddFromExisting<ILifecycleParticipant<ISiloLifecycle>, MetadataStoreMembershipTable>();
                        services.AddFromExisting<IMembershipTable, MetadataStoreMembershipTable>();
                        services.AddDynamicOptions<MetadataStoreClusteringOptions>();
                    })
                    .UseMetadataStore()
                    .UseMemoryLocalStore()
                    .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IMetadataStoreGrain).Assembly));
            }
        }

        public class ClientConfigurator : IClientBuilderConfigurator
        {
            public void Configure(IConfiguration configuration, IClientBuilder builder)
            {
                builder.ConfigureLogging(logging => logging.AddDebug());
                builder.ConfigureServices(services => services.AddDynamicOptions<StaticGatewayListProviderOptions>());
            }
        }





        // dynamic options

        // dynamic options

        // dynamic options

        // dynamic options

        // dynamic options

        // dynamic options

        // dynamic options

        // dynamic options

        // dynamic options






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
