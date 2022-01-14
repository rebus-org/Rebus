using System;
using System.Runtime.Serialization;

namespace Rebus.Exceptions;

/// <summary>
/// Fail-fast exception bypasses the retry logic and goes to the error queue directly
/// </summary>
[Serializable]
public class MessageCouldNotBeDispatchedToAnyHandlersException : RebusApplicationException, IFailFastException
{
    /// <summary>
    /// Constructs the exception with the given message
    /// </summary>
    public MessageCouldNotBeDispatchedToAnyHandlersException(string message) : base(message)
    {
    }

    /// <summary>
    /// Happy cross-domain serialization!
    /// </summary>
    public MessageCouldNotBeDispatchedToAnyHandlersException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}