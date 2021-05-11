using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Orleans.Internal;

namespace Orleans.Runtime.Placement
{
    internal class RandomPlacementDirector : IPlacementDirector
    {
        public virtual Task<SiloAddress> OnAddActivation(
            PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
        {
            var allSilos = context.GetCompatibleSilos(target);
            return Task.FromResult(allSilos[ThreadSafeRandom.Next(allSilos.Length)]);
        }
    }

    internal class FixedPlacementDirector : IPlacementDirector
    {
        public virtual Task<SiloAddress> OnAddActivation(
            PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
        {
            var targetSilo = FixedPlacement.ParseSiloAddress(target.GrainIdentity);
            var allSilos = context.GetCompatibleSilos(target);
            var found = false;
            foreach (var silo in allSilos)
            {
                if (silo.Equals(targetSilo))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                ThrowSiloUnavailable(target, targetSilo);
            }

            return Task.FromResult(targetSilo);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void ThrowSiloUnavailable(PlacementTarget target, SiloAddress targetSilo) => throw new SiloUnavailableException($"The silo {targetSilo} for grain {target.GrainIdentity} is not available");
        }
    }
}
