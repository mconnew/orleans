﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using Orleans;
using Orleans.Runtime;

namespace Microsoft.Orleans.ServiceFabric
{
    /// <summary>
    /// Monitors cluster changes for information about silos which are defunct and have not been reported as functional by Service Fabric.
    /// </summary>
    internal class UnknownSiloMonitor
    {
        /// <summary>
        /// Collection of unknown silos.
        /// </summary>
        private readonly ConcurrentDictionary<SiloAddress, DateTime> unknownSilos = new ConcurrentDictionary<SiloAddress, DateTime>();
        private readonly ServiceFabricMembershipOptions options;
        private readonly Logger log;

        public UnknownSiloMonitor(ServiceFabricMembershipOptions options, Factory<string, Logger> logFactory)
        {
            this.options = options;
            this.log = logFactory(nameof(UnknownSiloMonitor));
        }

        /// <summary>
        /// Gets or sets the delegate used to retrieve the current time.
        /// </summary>
        /// <remarks>
        /// This is exposed for testing purposes.
        /// The accuracy of this method
        /// </remarks>
        internal Func<DateTime> GetDateTime { get; set; } = () => DateTime.UtcNow;
        
        /// <summary>
        /// Adds an unknown silo to monitor.
        /// </summary>
        /// <param name="address">The silo address.</param>
        /// <returns><see langword="true"/> if the silo was added as an unknown silo, <see langword="false"/> otherwise.</returns>
        public bool ReportUnknownSilo(SiloAddress address)
        {
            return this.unknownSilos.TryAdd(address, this.GetDateTime());
        }

        /// <summary>
        /// Finds dead silos which were previously in an unknown state.
        /// </summary>
        /// <param name="allKnownSilos">The collection of all known silos, including dead silos.</param>
        /// <param name="isSingletonPartition">Whether or not this service is running inside of a singleton partition.</param>
        /// <returns>A collection of dead silos.</returns>
        public IEnumerable<SiloAddress> DetermineDeadSilos(Dictionary<SiloAddress, SiloStatus> allKnownSilos, bool isSingletonPartition)
        {
            if (this.unknownSilos.Count == 0) return Array.Empty<SiloAddress>();
            
            // The latest generation for each silo endpoint.
            var latestGenerations = new Dictionary<IPEndPoint, int>();

            // All known silos can be removed from the unknown list as long as their status is valid.
            foreach (var known in allKnownSilos)
            {
                if (known.Value == SiloStatus.None) continue;
                var address = known.Key;
                var endpoint = address.Endpoint;
                if (!latestGenerations.TryGetValue(endpoint, out var knownGeneration) || knownGeneration < address.Generation)
                {
                    latestGenerations[endpoint] = address.Generation;
                }

                this.unknownSilos.TryRemove(address, out var _);
            }

            var updates = new List<SiloAddress>();
            foreach (var pair in this.unknownSilos)
            {
                var unknownSilo = pair.Key;

                // If a known silo exists on the endpoint with a higher generation, the old silo must be dead.
                if (latestGenerations.TryGetValue(unknownSilo.Endpoint, out var knownGeneration) && knownGeneration > unknownSilo.Generation)
                {
                    this.log.Info($"Unknown silo {unknownSilo} was superseded by later generation on same endpoint {SiloAddress.New(unknownSilo.Endpoint, knownGeneration)}.");
                    updates.Add(unknownSilo);
                }

                // If this is a singleton partition, then any silo with a given address (excluding port) and a higher generation indicates that the
                // unknown silo is dead.
                if (isSingletonPartition)
                {
                    foreach (var knownSilo in latestGenerations)
                    {
                        if (unknownSilo.Endpoint.Address.Equals(knownSilo.Key.Address) && knownSilo.Value > unknownSilo.Generation)
                        {
                            this.log.Info($"Unknown silo {unknownSilo} was superseded by {knownSilo}.");
                            updates.Add(unknownSilo);
                        }
                    }
                }

                // Silos which have been in an unknown state for more than configured maximum allowed time are automatically considered dead.
                if (this.GetDateTime() - pair.Value > this.options.UnknownSiloRemovalPeriod)
                {
                    this.log.Info($"Unknown silo {unknownSilo} declared dead after {this.options.UnknownSiloRemovalPeriod}.");
                    updates.Add(unknownSilo);
                }
            }

            return updates;
        }
    }
}