using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkGrainInterfaces.Ping;
using BenchmarkGrains.Ping;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Threading;

namespace Benchmarks.Ping
{
#if NETCOREAPP
    internal sealed class NetCoreThreadPoolExecutor : IExecutor
    {
        public void QueueWorkItem(Action<object> callback, object state = null)
        {
            ThreadPool.UnsafeQueueUserWorkItem(callback, state, preferLocal: true);
        }
    }
#endif

    [MemoryDiagnoser]
    public class PingBenchmark : IDisposable 
    {
        private readonly List<ISiloHost> hosts = new List<ISiloHost>();
        private readonly IPingGrain grain;
        private readonly IClusterClient client;

        public PingBenchmark() : this(1, true) { }

        public PingBenchmark(int numSilos, bool startClient, bool grainsOnSecondariesOnly = false, bool dotnetThreadPool = false)
        {
            for (var i = 0; i < numSilos; ++i)
            {
                var primary = i == 0 ? null : new IPEndPoint(IPAddress.Loopback, 11111);
                var siloBuilder = new SiloHostBuilder()
                    .ConfigureDefaults()
                    .ConfigureServices((ctx, services) =>
                    {
                        if (dotnetThreadPool)
                        {
                            services.AddSingleton<IExecutor, NetCoreThreadPoolExecutor>();
                        }
                    })
                    .AddIncomingGrainCallFilter<RequestResponseGrainFilter>()
                    .AddOutgoingGrainCallFilter<RequestResponseGrainFilter>()
                    .Configure<SiloMessagingOptions>(o => o.PropagateActivityId = true)
                    .UseLocalhostClustering(
                        siloPort: 11111 + i,
                        gatewayPort: 30000 + i,
                        primarySiloEndpoint: primary);

                if (i == 0 && grainsOnSecondariesOnly)
                {
                    siloBuilder.ConfigureApplicationParts(parts =>
                        parts.AddApplicationPart(typeof(IPingGrain).Assembly));
                    siloBuilder.ConfigureServices(services =>
                    {
                        services.Remove(services.First(s => s.ImplementationType?.Name == "ApplicationPartValidator"));
                    });
                }
                else
                {
                    siloBuilder.ConfigureApplicationParts(parts =>
                        parts.AddApplicationPart(typeof(IPingGrain).Assembly)
                             .AddApplicationPart(typeof(PingGrain).Assembly));
                }

                var silo = siloBuilder.Build();
                silo.StartAsync().GetAwaiter().GetResult();
                this.hosts.Add(silo);
            }

            if (grainsOnSecondariesOnly) Thread.Sleep(4000);

            if (startClient)
            {
                var clientBuilder = new ClientBuilder()
                    .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IPingGrain).Assembly))
                    .Configure<ClusterOptions>(options => options.ClusterId = options.ServiceId = "dev")
                    .Configure<ClientMessagingOptions>(o => o.PropagateActivityId = true)
                    .AddOutgoingGrainCallFilter<RequestResponseGrainFilter>();

                if (numSilos == 1)
                {
                    clientBuilder.UseLocalhostClustering();
                }
                else
                {
                    var gateways = Enumerable.Range(30000, numSilos).Select(i => new IPEndPoint(IPAddress.Loopback, i)).ToArray();
                    clientBuilder.UseStaticClustering(gateways);
                }

                this.client = clientBuilder.Build();
                this.client.Connect().GetAwaiter().GetResult();
                var grainFactory = this.client;

                this.grain = grainFactory.GetGrain<IPingGrain>(Guid.NewGuid().GetHashCode());
                this.grain.Run().GetAwaiter().GetResult();
            }
        }
        
        [Benchmark]
        public Task Ping() => grain.Run();

        public async Task PingForever()
        {
            while (true)
            {
                await grain.Run();
            }
        }

        public Task PingConcurrent() => this.Run(
            runs: 3,
            grainFactory: this.client,
            blocksPerWorker: 10);

        public Task PingConcurrentHostedClient() => this.Run(
            runs: 3,
            grainFactory: (IGrainFactory)this.hosts[0].Services.GetService(typeof(IGrainFactory)),
            blocksPerWorker: 30);

        private async Task Run(int runs, IGrainFactory grainFactory, int blocksPerWorker)
        {
            var loadGenerator = new ConcurrentLoadGenerator<IPingGrain>(
                maxConcurrency: 250,
                blocksPerWorker: blocksPerWorker,
                requestsPerBlock: 500,
                issueRequest: g => g.Run(),
                getStateForWorker: workerId => grainFactory.GetGrain<IPingGrain>(Guid.NewGuid().GetHashCode()));
            await loadGenerator.Warmup();
            while (runs-- > 0) await loadGenerator.Run();
        }

        public async Task PingPongForever(CancellationToken cancellation)
        {
            var other = this.client.GetGrain<IPingGrain>(Guid.NewGuid().GetHashCode());
            while (!cancellation.IsCancellationRequested)
            {
                Trace.CorrelationManager.StartLogicalOperation();
                await grain.PingPongInterleave(other, 100);
                Trace.CorrelationManager.StopLogicalOperation();
            }
        }

        public async Task Shutdown()
        {
            if (this.client is IClusterClient c)
            {
                await c.Close();
                c.Dispose();
            }

            this.hosts.Reverse();
            foreach (var h in this.hosts)
            {
                await h.StopAsync();
                h.Dispose();
            }
        }

        [GlobalCleanup]
        public void Dispose()
        {
            (this.client as IDisposable)?.Dispose(); 
            this.hosts.ForEach(h => h.Dispose());
        }
    }

    public class RequestResponseGrainFilter : IIncomingGrainCallFilter, IOutgoingGrainCallFilter
    {
        public async Task Invoke(IOutgoingGrainCallContext context)
        {
            Console.WriteLine(EventSource.CurrentThreadActivityId);
            RequestResponseEventSource.Log.RequestStart();
            await context.Invoke();
            RequestResponseEventSource.Log.RequestStop();
        }

        public async Task Invoke(IIncomingGrainCallContext context)
        {
            RequestResponseEventSource.Log.InvokeStart();
            await context.Invoke();
            RequestResponseEventSource.Log.InvokeStop();
        }
    }

    [EventSource(Name = "Microsoft-Orleans-RPC")]
    public class RequestResponseEventSource : EventSource
    {
        public static readonly RequestResponseEventSource Log = new RequestResponseEventSource();

        [Event(1)]
        public void RequestStart() => this.WriteEvent(1);

        [Event(2)]
        public void RequestStop() => this.WriteEvent(2);

        [Event(3)]
        public void InvokeStart() => this.WriteEvent(3);

        [Event(4)]
        public void InvokeStop() => this.WriteEvent(4);
    }

    internal class OrleansEventTraceListener
    {
        public static async Task Run(CancellationToken cancellation)
        {
            Console.WriteLine("Starting trace session");
            if (!(TraceEventSession.IsElevated() is true)) throw new AccessViolationException("Must be run elevated in order to capture EventSource events");

            var pid = Process.GetCurrentProcess().Id;
            var fileName = $"{Assembly.GetExecutingAssembly().GetName().Name}_{DateTime.Now:yyyyMMdd_HHmmss_FFF}.etl";
            using var session = new TraceEventSession("OrleansDiagnostics", fileName);

            session.EnableProvider("Microsoft-Orleans-Dispatcher", TraceEventLevel.Always);
            session.EnableProvider("Microsoft-Orleans-InsideRuntimeClient", TraceEventLevel.Always);
            session.EnableProvider("Microsoft-Orleans-GatewayAcceptor", TraceEventLevel.Always);
            session.EnableProvider("Microsoft-Orleans-IncomingMessageAcceptor", TraceEventLevel.Always);
            session.EnableProvider("Microsoft-Orleans-IncomingMessageAgent", TraceEventLevel.Always);
            session.EnableProvider("Microsoft-Orleans-CallBackData", TraceEventLevel.Always);
            session.EnableProvider("Microsoft-Orleans-OutsideRuntimeClient", TraceEventLevel.Always);
            
            await cancellation.WhenCancelled();
            Console.WriteLine("Stopping trace session");
        }
    }

    public static class TaskExtensions
    {
        public static Task WhenCancelled(this CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            var waitForCancellation = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            token.Register(obj =>
            {
                var tcs = (TaskCompletionSource<object>)obj;
                tcs.TrySetResult(null);
            }, waitForCancellation);

            return waitForCancellation.Task;
        }
    }
}