using System;


namespace Orleans.Runtime
{
    internal class PlacementResult
    {
        public PlacementStrategy PlacementStrategy { get; private set; }

        public ActivationId Activation { get; private set; }
        public SiloAddress Silo { get; private set; }

        private PlacementResult()
        {
        }

        public static PlacementResult IdentifySelection(ActivationAddress address)
        {
            return new PlacementResult
            {
                Activation = address.Activation,
                Silo = address.Silo
            };
        }

        public static PlacementResult SpecifyCreation(
            SiloAddress silo,
            ActivationId activationId,
            PlacementStrategy placement)
        {
            if (silo == null)
                throw new ArgumentNullException(nameof(silo));
            if (activationId == null)
                throw new ArgumentNullException(nameof(activationId));
            if (placement == null)
                throw new ArgumentNullException(nameof(placement));

            return new PlacementResult
            {
                Activation = activationId,
                Silo = silo,
                PlacementStrategy = placement
            };
        }

        public ActivationAddress ToAddress(GrainId grainId) => ActivationAddress.GetAddress(Silo, grainId, Activation);

        public override string ToString() => $"PlacementResult({this.Silo}, {this.Activation}, {PlacementStrategy.ToString()})";
    }
}