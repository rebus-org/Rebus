using System;
#if NET45
using System.Runtime.Serialization;
#elif NETSTANDARD2_0
using System.Runtime.Serialization;
#endif

namespace Rebus.Exceptions
{
    /// <summary>
    /// Fail-fast exception bypasses the retry logic and goes to the error queue directly
    /// </summary>
#if NET45
    [Serializable]
#elif NETSTANDARD2_0
    [Serializable]
#endif
    public class MessageCouldNotBeDispatchedToAnyHandlersException : RebusApplicationException, IFailFastException
    {
        /// <summary>
        /// Constructs the exception with the given message
        /// </summary>
        public MessageCouldNotBeDispatchedToAnyHandlersException(string message) : base(message)
        {
        }

#if NET45
        /// <summary>
        /// Happy cross-domain serialization!
        /// </summary>
        public MessageCouldNotBeDispatchedToAnyHandlersException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#elif NETSTANDARD2_0
/// <summary>
/// Happy cross-domain serialization!
/// </summary>
        public MessageCouldNotBeDispatchedToAnyHandlersException(SerializationInfo info, StreamingContext context)
            :base(info, context)
        {
        }
#endif
    }
}