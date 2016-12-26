using System;
#if net46
using System.Runtime.Serialization;
#endif

namespace Rebus.Exceptions
{
    /// <summary>
    /// Generic application exception to use when something bad happens that is pretty unexpected and should be taken seriously
    /// </summary>
#if net46
    [Serializable]
#endif
    public class RebusApplicationException : Exception
    {
        /// <summary>
        /// Constructs the exception with the given message
        /// </summary>
        public RebusApplicationException(string message)
            :base(message)
        {
        }

        /// <summary>
        /// Constructs the exception with the given message and inner exception
        /// </summary>
        public RebusApplicationException(Exception innerException, string message)
            :base(message, innerException)
        {
        }

#if net46
        /// <summary>
        /// Happy cross-domain serialization!
        /// </summary>
        public RebusApplicationException(SerializationInfo info, StreamingContext context)
            :base(info, context)
        {
        }
#endif
    }
}