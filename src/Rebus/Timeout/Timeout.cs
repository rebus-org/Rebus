using System;

namespace Rebus.Timeout
{
    /// <summary>
    /// Represents a timeout 
    /// </summary>
    public class Timeout
    {
        /// <summary>
        /// Indicates to whom the reply should be sent
        /// </summary>
        public string ReplyTo { get; set; }
        
        /// <summary>
        /// Stores a correlation ID to be passed back in the reply
        /// </summary>
        public string CorrelationId { get; set; }
        
        /// <summary>
        /// Stores the UTC time of when to send the reply
        /// </summary>
        public DateTime TimeToReturn { get; set; }
        
        /// <summary>
        /// If applicable, stores the ID of the saga that sent the request. Will
        /// be passed back in the reply
        /// </summary>
        public Guid SagaId { get; set; }
        
        /// <summary>
        /// Stores a piece of custom data - possibly a serialized message to be unpacked
        /// and delivered by the recipient of the reply
        /// </summary>
        public string CustomData { get; set; }

        public override string ToString()
        {
            return string.Format("{0}: {1} -> {2}", TimeToReturn, CorrelationId, ReplyTo);
        }
    }
}