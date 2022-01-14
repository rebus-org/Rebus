using System;
using System.Collections.Generic;

namespace Rebus.Retry.Simple;

/// <summary>
/// Interface of the wrapper of a failed message
/// </summary>
public interface IFailed<out TMessage>
{
    /// <summary>
    /// Gets the message that failed
    /// </summary>
    TMessage Message { get; }

    /// <summary>
    /// Gets a (sometimes pretty long) description of the encountered error(s)
    /// </summary>
    string ErrorDescription { get; }

    /// <summary>
    /// Gets the headers of the message that failed
    /// </summary>
    Dictionary<string, string> Headers { get; }

    /// <summary>
    /// Gets all exceptions that were caught leading to this failed message
    /// </summary>
    IEnumerable<Exception> Exceptions { get; }
}