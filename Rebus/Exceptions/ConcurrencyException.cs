using System;
#if NET45
using System.Runtime.Serialization;
#endif

namespace Rebus.Exceptions
{
    /// <summary>
    /// Special exception that signals that some kind of optimistic lock has been violated, and work must most likely be aborted &amp; retried
    /// </summary>
#if NET45
    [Serializable]
#endif
    public class ConcurrencyException : Exception
    {
        /// <summary>
        /// Constructs the exception
        /// </summary>
        public ConcurrencyException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructs the exception
        /// </summary>
        public ConcurrencyException(Exception innerException, string message)
            : base(message, innerException)
        {
        }

#if NET45
        /// <summary>
        /// Constructs the exception
        /// </summary>
        public ConcurrencyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}