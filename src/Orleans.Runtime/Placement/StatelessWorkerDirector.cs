using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.Internal;

namespace Orleans.Runtime.Placement
{
    internal class StatelessWorkerDirector : IPlacementDirector, IActivationSelector
    {
        private static readonly SafeRandom random = new SafeRandom();

        public ValueTask<PlacementResult> OnSelectActivation(PlacementStrategy strategy, GrainId target, IPlacementRuntime context) => new ValueTask<PlacementResult>(SelectActivationCore(strategy, target, context));

        public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
        {
            var compatibleSilos = context.GetCompatibleSilos(target);

            // If the current silo is not shutting down, place locally if we are compatible
            if (!context.LocalSiloStatus.IsTerminating())
            {
                foreach (var silo in compatibleSilos)
                {
                    if (silo.Equals(context.LocalSilo))
                    {
                        return Task.FromResult(context.LocalSilo);
                    }
                }
            }

            // otherwise, place somewhere else
            return Task.FromResult(compatibleSilos[random.Next(compatibleSilos.Length)]);
        }

        private PlacementResult SelectActivationCore(PlacementStrategy strategy, GrainId target, IPlacementRuntime context)
        {
            if (target.IsClient())
                throw new InvalidOperationException("Cannot use StatelessWorkerStrategy to route messages to client grains.");

            // If there are available (not busy with a request) activations, it returns the first one.
            // If all are busy and the number of local activations reached or exceeded MaxLocal, it randomly returns one of them.
            // Otherwise, it requests creation of a new activation.
            if (context.LocalSiloStatus.IsTerminating())
            {
                return null;
            }

            if (!context.TryGetActivation(target, out var local))
            {
                return null;
            }

            return PlacementResult.IdentifySelection(ActivationAddress.GetAddress(context.LocalSilo, target, local.ActivationId));
        }

        internal static ActivationData PickRandom(List<ActivationData> local)
        {
            return local[local.Count == 1 ? 0 : random.Next(local.Count)];
        }
    }
}
