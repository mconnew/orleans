using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Orleans.MetadataStore;
using System;
using System.Threading;
using System.Threading.Tasks;

Host.CreateDefaultBuilder(args)
    .UseOrleans((ctx, siloBuilder) =>
    {
        siloBuilder.UseLocalhostClustering()
            .UseMetadataStore()
            .UseMemoryLocalStore();
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<CounterService>();
    })
    .RunConsoleAsync();

public class CounterService : BackgroundService
{
    private const string Key = "key";
    private readonly ILogger _log;
    private readonly ICounterGrain _grain;

    public CounterService(ILogger<CounterService> log, IGrainFactory grainFactory)
    {
        _grain = grainFactory.GetGrain<ICounterGrain>(Key);
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        CounterValue data = null;
        int iteration = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            ++iteration;

            if (data is null)
            {
                var readResult = await _grain.Get();
                if (!readResult.Success)
                {
                    _log.LogWarning("Read failed");
                    continue;
                }

                data = readResult.Value;
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
                if (iteration % 100 == 0)
                {
                    _log.LogInformation("{Iteration}: Value {Value} at version {Version}", iteration, data.Value, data.Version);
                }
            }
            catch (Exception exception)
            {
                _log.LogError(exception, "Exception in counter service");
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
