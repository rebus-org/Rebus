using System;

namespace Rebus.Messages
{
    /// <summary>
    /// This is the reply that the Timeout Service will send back to the
    /// timeout requestor upon completion of the timeout.
    /// </summary>
    [Serializable]
    public class TimeoutReply : IRebusControlMessage
    {
        /// <summary>
        /// The UTC time of when the timeout expired.
        /// </summary>
        public DateTime DueTime { get; set; }

        /// <summary>
        /// The correlation ID as specified in the <see cref="TimeoutRequest"/>.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// The saga ID as specified in the <see cref="TimeoutRequest"/>.
        /// </summary>
        public Guid SagaId { get; set; }

        /// <summary>
        /// The custom data as specified in the <see cref="TimeoutRequest"/>.
        /// </summary>
        public string CustomData { get; set; }
    }
}