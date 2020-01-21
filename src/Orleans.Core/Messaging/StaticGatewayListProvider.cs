using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Orleans.Messaging
{
    public class StaticGatewayListProvider : IGatewayListProvider, IGatewayListObservable
    {
        private readonly object lockObj = new object();
        private readonly IOptionsMonitor<StaticGatewayListProviderOptions> options;
        private readonly ILogger<StaticGatewayListProvider> log;
        private ImmutableHashSet<IGatewayListListener> listeners = ImmutableHashSet.Create<IGatewayListListener>(ReferenceEqualsComparer<IGatewayListListener>.Instance);

        public StaticGatewayListProvider(
            IOptionsMonitor<StaticGatewayListProviderOptions> options,
            IOptions<GatewayOptions> gatewayOptions,
            ILogger<StaticGatewayListProvider> log)
        {
            this.options = options;
            this.log = log;
            this.MaxStaleness = gatewayOptions.Value.GatewayListRefreshPeriod;

            options.OnChange(snapshot =>
            {
                var listenersCopy = this.listeners;
                foreach (var listener in listenersCopy)
                {
                    try
                    {
                        listener.GatewayListNotification(snapshot.Gateways);
                    }
                    catch (Exception exception)
                    {
                        this.log.LogError(exception, "Error while updating gateway listener {Listener}", listener);
                    }
                }
            });
        }

        public Task InitializeGatewayListProvider() => Task.CompletedTask;
        
        public Task<IList<Uri>> GetGateways() => Task.FromResult<IList<Uri>>(this.options.CurrentValue.Gateways);

        public bool SubscribeToGatewayNotificationEvents(IGatewayListListener listener)
        {
            lock (this.lockObj)
            {
                this.listeners = this.listeners.Add(listener);
                return true;
            }
        }

        public bool UnSubscribeFromGatewayNotificationEvents(IGatewayListListener listener)
        {
            lock (this.lockObj)
            {
                this.listeners = this.listeners.Remove(listener);
                return true;
            }
        }

        public TimeSpan MaxStaleness { get; private set; }

        public bool IsUpdatable => true;
    }
}
