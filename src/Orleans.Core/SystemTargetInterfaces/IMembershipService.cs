using System;
using System.Threading.Tasks;


namespace Orleans.Runtime
{
    internal interface IMembershipService : ISystemTarget
    {
        /// <summary>
        /// Receive notifications about silo status events. 
        /// </summary>
        /// <param name="updatedSilo">Silo to update about</param>
        /// <param name="status">Status of the silo</param>
        /// <returns></returns>
        /// TODO REMOVE in a next version
        Task SiloStatusChangeNotification(SiloAddress updatedSilo, SiloStatus status);

        /// <summary>
        /// Receive notifications about a change in the membership table
        /// </summary>
        /// <param name="snapshot">Snapshot of the membership table</param>
        /// <returns></returns>
        Task MembershipChangeNotification(MembershipTableSnapshot snapshot);

        /// <summary>
        /// Ping request from another silo that probes the liveness of the recipient silo.
        /// </summary>
        /// <param name="pingNumber">A unique sequence number for ping message, to facilitate testing and debugging.</param>
        Task Ping(int pingNumber);

        Task<IndirectProbeResponse> ProbeIndirectly(SiloAddress target, TimeSpan probeTimeout, int probeNumber);
    }

    /// <summary>
    /// Represents the result of probing a node via an intermediary node.
    /// </summary>
    [Serializable]
    [Orleans.GenerateSerializer]
    public struct IndirectProbeResponse
    {
        /// <summary>
        /// The health score of the intermediary node.
        /// </summary>
        [Orleans.Id(1)]
        public int IntermediaryHealthScore { get; set; }

        /// <summary>
        /// <see langword="true"/> if the probe succeeded; otherwise, <see langword="false"/>.
        /// </summary>
        [Orleans.Id(2)]
        public bool Succeeded { get; set; }

        /// <summary>
        /// The duration of the probe attempt.
        /// </summary>
        [Orleans.Id(3)]
        public TimeSpan ProbeResponseTime { get; set; }

        /// <summary>
        /// The failure message if the probe did not succeed.
        /// </summary>
        [Orleans.Id(4)]
        public string FailureMessage { get; set; }

        /// <inheritdoc />
        public override string ToString() => $"IndirectProbeResponse {{ Succeeded: {Succeeded}, IntermediaryHealthScore: {IntermediaryHealthScore}, ProbeResponseTime: {ProbeResponseTime}, FailureMessage: {FailureMessage} }}";
    }
}
