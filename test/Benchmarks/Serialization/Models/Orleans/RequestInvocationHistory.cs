using System;

namespace FakeFx.Runtime
{
    // used for tracking request invocation history for deadlock detection.
    [Serializable]
    [Orleans.Serialization.GenerateSerializer]
    internal sealed class RequestInvocationHistory
    {
        [Orleans.Serialization.Id(1)]
        public GrainId GrainId { get; private set; }
        [Orleans.Serialization.Id(2)]
        public ActivationId ActivationId { get; private set; }

        [Obsolete("Removed and unused. This member is retained only for serialization compatibility purposes.")]
        [Orleans.Serialization.Id(3)]
        public string DebugContext { get; private set; }

        public RequestInvocationHistory(GrainId grainId, ActivationId activationId)
        {
            this.GrainId = grainId;
            this.ActivationId = activationId;
        }

        public override string ToString() => $"RequestInvocationHistory {GrainId}:{ActivationId}";
    }
}
