using System;

namespace Rebus.Timeout
{
    /// <summary>
    /// Represents a timeout 
    /// </summary>
    public class Timeout
    {
        /// <summary>
        /// Constructs the due timeout with the specified values
        /// </summary>
        public Timeout(string replyTo, string correlationId, DateTime timeToReturn, Guid sagaId, string customData)
        {
            ReplyTo = replyTo;
            CorrelationId = correlationId;
            TimeToReturn = timeToReturn;
            SagaId = sagaId;
            CustomData = customData;
        }

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

        /// <summary>
        /// Gets a nifty string representation of this timeout
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}: {1} -> {2}", TimeToReturn, CorrelationId, ReplyTo);
        }
    }

    /// <summary>
    /// Extends a timeout to become a due timeout, which is a timeput that can be marked as processed
    /// </summary>
    public abstract class DueTimeout : Timeout
    {
        /// <summary>
        /// COnstructs the timeout
        /// </summary>
        protected DueTimeout(string replyTo, string correlationId, DateTime timeToReturn, Guid sagaId, string customData)
            : base(replyTo, correlationId, timeToReturn, sagaId, customData)
        {
        }

        /// <summary>
        /// Marks the timeout as processed, most likely removing it from the underlying timeout store
        /// </summary>
        public abstract void MarkAsProcessed();
    }
}