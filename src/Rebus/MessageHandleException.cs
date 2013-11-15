using System;
using System.Runtime.Serialization;

namespace Rebus
{
    /// <summary>
    /// Special exception that wraps an exception that occurred while handling a message
    /// </summary>
    [Serializable]
    public class MessageHandleException : ApplicationException
    {
        /// <summary>
        /// Mandatory exception ctor
        /// </summary>
        protected MessageHandleException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Constructs the exception for the given message ID and exception that was caught while handling that message
        /// </summary>
        public MessageHandleException(string messageId, Exception caughtException)
            :base(string.Format("Could not handle message with ID {0}", messageId), GetRealCaughtException(caughtException))
        {
            MessageId = messageId;
        }

        static Exception GetRealCaughtException(Exception caughtException)
        {
            return caughtException;
        }

        /// <summary>
        /// Gets the ID of the message that could not be handled
        /// </summary>
        public string MessageId { get; private set; }
    }
}