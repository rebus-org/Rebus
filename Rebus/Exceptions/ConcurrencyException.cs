using System;
using System.Runtime.Serialization;

namespace Rebus.Exceptions
{
    /// <summary>
    /// Special exception that signals that some kind of optimistic lock has been violated, and work must most likely be aborted & retried
    /// </summary>
    [Serializable]
    public class ConcurrencyException : ApplicationException
    {
        public ConcurrencyException(string message, params object[] objs)
            : base(string.Format(message, objs))
        {
        }

        public ConcurrencyException(Exception innerException, string message, params object[] objs)
            : base(string.Format(message, objs), innerException)
        {
        }

        public ConcurrencyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}