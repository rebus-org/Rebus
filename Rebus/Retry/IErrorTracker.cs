using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rebus.Retry;

/// <summary>
/// Service that is responsible for tracking errors across message deliveries.
/// </summary>
public interface IErrorTracker
{
    /// <summary>
    /// This method is called on each experienced failed delivery. 
    /// </summary>
    Task RegisterError(string messageId, Exception exception);

    /// <summary>
    /// This method is called when there's no need to track the error anymore
    /// </summary>
    Task CleanUp(string messageId);

    /// <summary>
    /// Gets whether the given message ID has had too many error registered for it
    /// </summary>
    Task<bool> HasFailedTooManyTimes(string messageId);

    /// <summary>
    /// Should get a full, detailed error description for the message ID (i.e. could be timestamps and full stack traces for all failed deliveries)
    /// </summary>
    Task<string> GetFullErrorDescription(string messageId);

    /// <summary>
    /// Gets all caught exceptions for the message ID
    /// </summary>
    Task<IReadOnlyList<Exception>> GetExceptions(string messageId);

    /// <summary>
    /// Marks the given <paramref name="messageId"/> as "FINAL", meaning that it should be considered as "having failed too many times now"
    /// </summary>
    Task MarkAsFinal(string messageId);
}