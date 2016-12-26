using System;
using System.Runtime.Serialization;

namespace Rebus.Exceptions
{
    /// <summary>
    /// Special exception that signals that some kind of optimistic lock has been violated, and work must most likely be aborted &amp; retried
    /// </summary>
#if net46
    [Serializable]
#endif
    public class ConcurrencyException : Exception
    {
        /// <summary>
        /// Constructs the exception
        /// </summary>
        public ConcurrencyException(string message, params object[] objs)
            : base(string.Format(message, objs))
        {
        }

        /// <summary>
        /// Constructs the exception
        /// </summary>
        public ConcurrencyException(Exception innerException, string message, params object[] objs)
            : base(string.Format(message, objs), innerException)
        {
        }

#if net46
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