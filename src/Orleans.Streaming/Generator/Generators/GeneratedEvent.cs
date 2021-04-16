using System;

namespace Orleans.Providers.Streams.Generator
{
    /// <summary>
    /// Event use in generated streams
    /// </summary>
    [Serializable]
    [Orleans.GenerateSerializer]
    public class GeneratedEvent
    {
        /// <summary>
        /// Generated event type
        /// </summary>
        public enum GeneratedEventType
        {
            /// <summary>
            /// filler event
            /// </summary>
            Fill,
            /// <summary>
            /// Event that should trigger reporting
            /// </summary>
            Report
        }

        /// <summary>
        /// Event type
        /// </summary>
        [Orleans.Id(0)]
        public GeneratedEventType EventType { get; set; }

        /// <summary>
        /// Event payload
        /// </summary>
        [Orleans.Id(1)]
        public int[] Payload { get; set; }
    }
}
