using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Orleans.MetadataStore;
using Orleans.Runtime;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

await Host.CreateDefaultBuilder(args)
    .UseOrleans((ctx, siloBuilder) =>
    {
        int.TryParse(ctx.Configuration["ID"], out var id);

        var port = 11111 + id;
        IPEndPoint primaryEndpoint = default;
        if (id > 0)
        {
            primaryEndpoint = new IPEndPoint(IPAddress.Loopback, 11111);
        }

        siloBuilder.UseLocalhostClustering(siloPort: port, gatewayPort: 0, primarySiloEndpoint: primaryEndpoint)
            .UseMetadataStore()
            .UseMemoryLocalStore();
        siloBuilder.ConfigureServices(services =>
        {
            services.AddHostedService<ClusterBootstrapper>();
            services.AddHostedService<CounterService>();
        });
    })
    .RunConsoleAsync();

public interface ICounterLoopGrain : IGrain
{
    ValueTask<(bool Success, long Version, int Value, int Iteration)> GetCount();
}

[GrainType("looper")]
public class CounterLoopGrain : Grain, ICounterLoopGrain
{
    private readonly CancellationTokenSource _cancellation = new();
    private ICounterGrain _grain;
    private ILogger _log;
    private int _iteration;
    private Task _runTask;

    public CounterLoopGrain(ILogger<CounterLoopGrain> log) => _log = log;

    public override Task OnActivateAsync()
    {
        _grain = GrainFactory.GetGrain<ICounterGrain>("key");
        _runTask = Run();
        return base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        _cancellation.Cancel();
        if (_runTask is { } task)
        {
            await task;
        }

        await base.OnActivateAsync();
    }

    public async ValueTask<(bool Success, long Version, int Value, int Iteration)> GetCount()
    {
        var result = await _grain.Get();
        return (result.Success, result.Value?.Version ?? 0, result.Value?.Value ?? 0, _iteration);
    }

    private async Task Run()
    {
        await Task.Yield();

        CounterValue data = null;
        while (!_cancellation.IsCancellationRequested)
        {
            ++_iteration;

            if (data is null)
            {
                var readResult = await _grain.Get();
                if (!readResult.Success)
                {
                    _log.LogWarning("Read failed");
                    continue;
                }

                data = readResult.Value ?? new CounterValue(0, 0);
            }

            try
            {
                // Increment the version.
                data = data with { Version = data.Version + 1, Value = data.Value + 1 };

                var updateResult = await _grain.TryUpdate(data);
                if (!updateResult.Success)
                {
                    _log.LogWarning("Update failed");
                }

                data = updateResult.Value;
                if (_iteration % 10000 == 0)
                {
                    _log.LogInformation("{Iteration}: Value {Value} at version {Version}", _iteration, data.Value, data.Version);
                }
            }
            catch (Exception exception)
            {
                _log.LogError(exception, "Exception in counter service");
                await Task.Delay(5000);
            }
        }
    }
}

public class CounterService : BackgroundService
{
    private readonly ILogger _log;
    private readonly ICounterLoopGrain _grain;

    public CounterService(ILogger<CounterService> log, IGrainFactory grainFactory)
    {
        _grain = grainFactory.GetGrain<ICounterLoopGrain>(GrainId.Create("looper", "singleton"));
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var (success, version, value, grainIteration) = await _grain.GetCount();
                _log.LogInformation("{Iteration}: Success {Success} Value {Value} at version {Version}", grainIteration, success, value, version);
                await Task.Delay(1000);
            }
            catch (Exception exception)
            {
                _log.LogError(exception, "Exception in counter service");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}

public abstract class KeyValueGrain<TValue> : Grain where TValue : class, IVersioned
{
    private readonly IMetadataStore _store;

    public KeyValueGrain(IMetadataStore store) => _store = store;

    protected Task<ReadResult<TValue>> Get(string key) => _store.TryGet<TValue>(key);

    protected Task<UpdateResult<TValue>> TryUpdate(string key, TValue updated) => _store.TryUpdate(key, updated);
}

public interface ICounterGrain : IGrainWithStringKey
{
    Task<ReadResult<CounterValue>> Get();

    Task<UpdateResult<CounterValue>> TryUpdate(CounterValue updated);
}

public class CounterGrain : KeyValueGrain<CounterValue>, ICounterGrain
{
    private string _key;
    public CounterGrain(IMetadataStore store) : base(store)
    {
    }

    public override Task OnActivateAsync()
    {
        _key = this.GetPrimaryKeyString();
        return base.OnActivateAsync();
    }

    public Task<ReadResult<CounterValue>> Get() => base.Get(_key);

    public Task<UpdateResult<CounterValue>> TryUpdate(CounterValue updated) => base.TryUpdate(_key, updated);
}

[Immutable]
[GenerateSerializer]
public record CounterValue([field: Id(0)] long Version, [field: Id(1)] int Value) : IVersioned
{
    public override string ToString()
    {
        return $"{nameof(Version)}: {Version}, {nameof(CounterValue)}: {Value}";
    }
}

internal class ClusterBootstrapper : BackgroundService
{
    private readonly ILocalSiloDetails _localSiloDetails;
    private readonly ConfigurationManager _configurationManager;
    private readonly IClusterMembershipService _membershipService;
    private readonly ILogger<ClusterBootstrapper> _log;
    private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

    public ClusterBootstrapper(
        ILocalSiloDetails localSiloDetails,
        ConfigurationManager configurationManager,
        IClusterMembershipService membershipService,
        ILogger<ClusterBootstrapper> log)
    {
        _localSiloDetails = localSiloDetails;
        _configurationManager = configurationManager;
        _membershipService = membershipService;
        _log = log;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_configurationManager.AcceptedConfiguration?.Configuration is null)
        {
            await _configurationManager.ForceLocalConfiguration(
                new ReplicaSetConfiguration(new Ballot(1, _configurationManager.NodeId),
                1,
                new[] {_localSiloDetails.SiloAddress},
                1,
                1));
        }

        await _configurationManager.TryAddServer(_localSiloDetails.SiloAddress);

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var previous = default(ClusterMembershipSnapshot);
            await foreach (var snapshot in _membershipService.MembershipUpdates.WithCancellation(_cancellation.Token))
            {
                var update = previous is null ? snapshot.AsUpdate() : snapshot.CreateUpdate(previous);

                // Process removals first.
                foreach (var change in update.Changes)
                {
                    if (change.Status.IsTerminating())
                    {
                        continue;
                    }

                    await ProcessUpdate(change);
                }

                // Process additions afterwards.
                foreach (var change in update.Changes)
                {
                    if (!change.Status.IsTerminating())
                    {
                        continue;
                    }

                    await ProcessUpdate(change);
                }
            }
        }
        catch (Exception exception)
        {
            _log.LogError(exception, "Exception updating configuration");
            await Task.Delay(5000);
        }
    }

    private async Task ProcessUpdate(ClusterMember update)
    {
        while (true)
        {
            var (silo, status) = (update.SiloAddress, update.Status);
            try
            {
                _log.LogInformation($"Got silo update: {silo} -> {status}");
                var reference = silo;
                UpdateResult<ReplicaSetConfiguration> result;
                switch (status)
                {
                    case SiloStatus.Active:
                        result = await _configurationManager.TryAddServer(reference);
                        break;
                    case SiloStatus.Dead:
                        result = await _configurationManager.TryRemoveServer(reference);
                        break;
                    default:
                        return;
                }

                _log.LogInformation($"Update result: {result}");

                // Continue until a successful result is obtained.
                if (result.Success)
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
            catch (Exception exception)
            {
                _log.LogError($"Exception processing update ({silo}, {status}): {exception}");
                await Task.Delay(5000);
            }
        }
    }
}
