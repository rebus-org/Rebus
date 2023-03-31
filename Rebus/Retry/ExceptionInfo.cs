using System;
using Rebus.Extensions;

namespace Rebus.Retry;

/// <summary>
/// Represents an <see cref="Exception"/>
/// </summary>
public record ExceptionInfo(string Type, string Message, string Details, DateTimeOffset Time)
{
    /// <summary>
    /// Creates an <see cref="ExceptionInfo"/> for the given <paramref name="exception"/>
    /// </summary>
    public static ExceptionInfo FromException(Exception exception)
    {
        if (exception == null) throw new ArgumentNullException(nameof(exception));

        return new(
            Type: exception.GetType().GetSimpleAssemblyQualifiedName(),
            Message: exception.Message,
            Details: exception.ToString(),
            Time: DateTimeOffset.Now
        );
    }

    /// <summary>
    /// Gets the full details of this exception info
    /// </summary>
    public string GetFullErrorDescription() => $"{Time}: {Details}";
}