using System;


namespace Orleans.Runtime
{
    internal class PlacementResult
    {
        public PlacementStrategy PlacementStrategy { get; private set; }

        public bool IsNewPlacement => PlacementStrategy != null;

        public SiloAddress Silo { get; private set; }

        private PlacementResult()
        {
        }

        public static PlacementResult IdentifySelection(ActivationAddress address)
        {
            return new PlacementResult
            {
                Silo = address.Silo
            };
        }

        public static PlacementResult SpecifyCreation(
            SiloAddress silo,
            PlacementStrategy placement)
        {
            if (silo == null)
                throw new ArgumentNullException(nameof(silo));

            if (placement == null)
                throw new ArgumentNullException(nameof(placement));

            return new PlacementResult
            {
                Silo = silo,
                PlacementStrategy = placement
            };
        }

        public ActivationAddress ToAddress(GrainId grainId)
        {
            return ActivationAddress.GetAddress(Silo, grainId);
        }

        public override string ToString()
        {
            var placementStr = IsNewPlacement ? PlacementStrategy.ToString() : "*not-new*";
            return $"PlacementResult({this.Silo}, {placementStr})";
        }
    }
}