using System;
using System.Runtime.CompilerServices;
using Orleans.Concurrency;

namespace Orleans.Runtime
{
    [Serializable, Immutable]
    [Hagar.GenerateSerializer]
    [Hagar.SuppressReferenceTracking]
    public sealed class ActivationAddress : IEquatable<ActivationAddress>
    {
        private static readonly Interner<(SiloAddress, GrainId, ActivationId), ActivationAddress> Interner = new();
        private static readonly Func<(SiloAddress, GrainId, ActivationId), ActivationAddress> CreateActivation = ((SiloAddress Silo, GrainId Grain, ActivationId Activation) key) => new ActivationAddress(key.Silo, key.Grain, key.Activation);

        [Hagar.Id(1)]
        public GrainId Grain { get; private set; }
        [Hagar.Id(2)]
        public ActivationId Activation { get; private set; }
        [Hagar.Id(3)]
        public SiloAddress Silo { get; private set; }

        public bool IsComplete => !Grain.IsDefault && Activation != null && Silo != null;

        private ActivationAddress(SiloAddress silo, GrainId grain, ActivationId activation)
        {
            Silo = silo;
            Grain = grain;
            Activation = activation;
        }

        public static ActivationAddress NewActivationAddress(SiloAddress silo, GrainId grain)
        {
            var activation = ActivationId.NewId();
            return GetAddress(silo, grain, activation);
        }

        public static ActivationAddress GetAddress(SiloAddress silo, GrainId grain, ActivationId activation)
        {
            // Silo part is not mandatory
            if (grain.IsDefault) ThrowArgumentException();

            return Interner.FindOrCreate((silo, grain, activation), CreateActivation);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void ThrowArgumentException() => throw new ArgumentNullException("grain");
        }

        public override bool Equals(object obj) => Equals(obj as ActivationAddress);

        public bool Equals(ActivationAddress other) => other != null && Matches(other) && (Silo?.Equals(other.Silo) ?? other.Silo is null);

        public override int GetHashCode() => Grain.GetHashCode() ^ (Activation?.GetHashCode() ?? 0) ^ (Silo?.GetHashCode() ?? 0);

        public override string ToString() => $"[{Silo} {Grain} {Activation}]";

        public string ToFullString()
        {
            return
                String.Format(
                    "[ActivationAddress: {0}, Full GrainId: {1}, Full ActivationId: {2}]",
                    this.ToString(),                        // 0
                    this.Grain.ToString(),                  // 1
                    this.Activation.ToFullString());        // 2
        }

        public bool Matches(ActivationAddress other)
        {
            return Grain.Equals(other.Grain) && (Activation?.Equals(other.Activation) ?? other.Activation is null);
        }
    }
}
