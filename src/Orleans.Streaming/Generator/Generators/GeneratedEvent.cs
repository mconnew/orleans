using System;

namespace Orleans.Providers.Streams.Generator
{
    /// <summary>
    /// Event use in generated streams
    /// </summary>
    [Serializable]
    [Hagar.GenerateSerializer]
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
        [Hagar.Id(0)]
        public GeneratedEventType EventType { get; set; }

        /// <summary>
        /// Event payload
        /// </summary>
        [Hagar.Id(1)]
        public int[] Payload { get; set; }
    }
}
