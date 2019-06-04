using System;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Orleans.Runtime.MembershipService
{
    internal sealed class ClusterMembershipObserverManager
    {
        private readonly ILogger logger;
        private ImmutableHashSet<IClusterMembershipObserver> subscribers = ImmutableHashSet.Create<IClusterMembershipObserver>(ReferenceEqualsComparer<IClusterMembershipObserver>.Instance);

        public ClusterMembershipObserverManager(ILogger logger)
        {
            this.logger = logger;
        }

        public void Subscribe(IClusterMembershipObserver observer)
        {
            ImmutableHashSet<IClusterMembershipObserver> existing;
            ImmutableHashSet<IClusterMembershipObserver> updated;
            do
            {
                existing = this.subscribers;
                updated = existing.Add(observer);
            } while (!ReferenceEquals(Interlocked.CompareExchange(ref this.subscribers, updated, existing), existing));
        }

        public void Unsubscribe(IClusterMembershipObserver observer)
        {
            ImmutableHashSet<IClusterMembershipObserver> existing;
            ImmutableHashSet<IClusterMembershipObserver> updated;
            do
            {
                existing = this.subscribers;
                updated = existing.Remove(observer);
            } while (!ReferenceEquals(Interlocked.CompareExchange(ref this.subscribers, updated, existing), existing));
        }

        public async Task NotifyObservers(ClusterMembershipUpdate notification)
        {
            foreach (var subscriber in this.subscribers)
            {
                try
                {
                    await subscriber.OnClusterMembershipChange(notification).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    this.logger.LogWarning("Exception notifying cluster membership observer {Observer}: {Exception}", subscriber, exception);
                }
            }
        }
    }
}
