using System;
using System.Runtime.Serialization;

namespace Rebus.Exceptions;

/// <summary>
/// Fail-fast exception bypasses the retry logic and goes to the error queue directly
/// </summary>
[Serializable]
public class FailFastException : Exception, IFailFastException
{
    /// <summary>
    /// Constructs the exception with the given message
    /// </summary>
    public FailFastException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Constructs the exception with the given message and inner exception
    /// </summary>
    public FailFastException(Exception innerException, string message)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Happy cross-domain serialization!
    /// </summary>
    public FailFastException(SerializationInfo info, StreamingContext context)
        :base(info, context)
    {
    }
}