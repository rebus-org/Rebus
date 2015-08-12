using System;

namespace Rebus.Messages
{
    /// <summary>
    /// Requests a delayed reply from the Timeout Service. Upon receiving
    /// this message, the Timeout Service will calculate the UTC time of when the timeout
    /// should expire, wait, and then reply with a <see cref="TimeoutReply"/>.,
    /// </summary>
    [Serializable]
    public class TimeoutRequest : IRebusControlMessage
    {
        /// <summary>
        /// For how long should the reply be delayed?
        /// </summary>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// Allows for specifying a correlation ID that the Timeout Service will
        /// return with the <see cref="TimeoutReply"/>.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Allows for additional data to be passed along with the timeout. If you really really want,
        /// you COULD use this field to pass a serialized object.
        /// </summary>
        public string CustomData { get; set; }
        
        /// <summary>
        /// Allows for specifying the ID for the saga requesting the timeout.
        /// The ID will be returned with the <see cref="TimeoutReply"/>.
        /// </summary>
        public Guid SagaId { get; set; }
    }
}