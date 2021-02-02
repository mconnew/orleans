using System;
using Orleans.Concurrency;

namespace Orleans.Runtime
{
    [Serializable, Immutable]
    public sealed class ActivationAddress : IEquatable<ActivationAddress>
    {
        public ActivationAddress(GrainId grainId, SiloAddress siloAddress, string eTag)
        {
            Grain = grainId;
            Silo = siloAddress;
            ETag = eTag;
        }

        public ActivationAddress()
        { }

        public static ActivationAddress GetAddress(GrainId grainId, SiloAddress siloAddress) => new ActivationAddress(grainId, siloAddress, eTag: null);

        public static ActivationAddress GetAddress(GrainId grainId, SiloAddress siloAddress, string eTag) => new ActivationAddress(grainId, siloAddress, eTag);

        /// <summary>
        /// Identifier of the Grain
        /// </summary>
        public GrainId Grain { get; set; }

        /// <summary>
        /// Address of the silo where the grain activation lives
        /// </summary>
        public SiloAddress Silo { get; set; }

        /// <summary>
        /// ETag used for concurrency control
        /// </summary>
        public string ETag { get; set; }

        public bool Matches(ActivationAddress other) => this.Silo.Equals(other.Silo) && this.Grain.Equals(other.Grain);

        public override bool Equals(object obj) => obj is ActivationAddress address && this.Equals(address);

        public bool Equals(ActivationAddress other) => this.Matches(other) && string.Equals(this.ETag, other.ETag, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode() => HashCode.Combine(this.Silo, this.Grain, this.ETag);

        public override string ToString() => $"[{Grain} on {Silo} (ETag: \"{ETag?.ToString() ?? "null"}\")]";
    }
}
