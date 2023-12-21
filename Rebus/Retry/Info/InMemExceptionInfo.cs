using Rebus.Extensions;
using System;

namespace Rebus.Retry.Info;

/// <summary>
/// An in-memory exception info for in-memory error tracking.
/// </summary>
public record InMemExceptionInfo : ExceptionInfo
{
    /// <summary>
    /// Constructs a new in-memory exception info from a source exception.
    /// </summary>
    /// <param name="exception">Source exception.</param>
    public InMemExceptionInfo(Exception exception) : base(
        exception?.GetType().GetSimpleAssemblyQualifiedName(),
        exception?.Message,
        exception?.ToString(),
        DateTimeOffset.Now)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }

    /// <summary>
    /// Gets or sets the original exception.
    /// </summary>
    public Exception Exception { get; }
}