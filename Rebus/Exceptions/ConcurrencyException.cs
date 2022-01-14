using System;
using System.Runtime.Serialization;

namespace Rebus.Exceptions;

/// <summary>
/// Special exception that signals that some kind of optimistic lock has been violated, and work must most likely be aborted &amp; retried
/// </summary>
[Serializable]
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

    /// <summary>
    /// Constructs the exception
    /// </summary>
    public ConcurrencyException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}