using System;
using System.Runtime.Serialization;

namespace Rebus.Bus
{
    /// <summary>
    /// Special exception that wraps an exception that occurred while committing the current queue transaction
    /// </summary>
    [Serializable]
    public class QueueCommitException : ApplicationException
    {
        /// <summary>
        /// Mandatory exception ctor
        /// </summary>
        protected QueueCommitException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Constructs the queue commit exception with a message containint the exception that was caught when trying to
        /// commit the queue transaction
        /// </summary>
        public QueueCommitException(Exception innerException)
            : base("An exception occurred while attempting to commit the queue transaction", innerException)
        {
        }
    }
}