using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Retry.Simple;

/// <summary>
/// Wraps a failed message that is to be retried
/// </summary>
class FailedMessageWrapper<TMessage> : IFailed<TMessage>
{
    /// <summary>
    /// Gets the message that failed
    /// </summary>
    public TMessage Message { get; }

    /// <summary>
    /// Gets a (sometimes pretty long) description of the encountered error(s)
    /// </summary>
    public string ErrorDescription { get; }

    /// <summary>
    /// Gets all exceptions that were caught leading to this <see cref="IFailed{TMessage}"/>
    /// </summary>
    public IEnumerable<ExceptionInfo> Exceptions { get; }

    /// <summary>
    /// Gets the headers of the message that failed
    /// </summary>
    public Dictionary<string, string> Headers { get; }

    /// <summary>
    /// Constructs the wrapper with the given message
    /// </summary>
    public FailedMessageWrapper(Dictionary<string, string> headers, TMessage message, string errorDescription, IEnumerable<ExceptionInfo> exceptions)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (exceptions == null) throw new ArgumentNullException(nameof(exceptions));
        Headers = headers ?? throw new ArgumentNullException(nameof(headers));
        Message = message;
        ErrorDescription = errorDescription ?? throw new ArgumentNullException(nameof(errorDescription));
        Exceptions = exceptions.ToArray();
    }

    /// <summary>
    /// Returns a string that represents the current failed message
    /// </summary>
    public override string ToString() => $"FAILED: {Message}";
}