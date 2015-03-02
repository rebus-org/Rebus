using System;
using System.Runtime.Serialization;

namespace Rebus2.Exceptions
{
    [Serializable]
    public class ConcurrencyException : ApplicationException
    {
        public ConcurrencyException(string message, params object[] objs)
            : base(string.Format(message, objs))
        {
        }

        public ConcurrencyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}