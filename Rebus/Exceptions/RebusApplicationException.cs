using System;

namespace Rebus.Exceptions;

/// <summary>
/// Generic application exception to use when something bad happens that is pretty unexpected and should be taken seriously
/// </summary>
[Serializable]
public class RebusApplicationException : Exception
{
    /// <summary>
    /// Constructs the exception with the given message
    /// </summary>
    public RebusApplicationException(string message) :base(message)
    {
    }

    /// <summary>
    /// Constructs the exception with the given message and inner exception
    /// </summary>
    public RebusApplicationException(Exception innerException, string message) :base(message, innerException)
    {
    }
}