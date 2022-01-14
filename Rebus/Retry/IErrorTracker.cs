using System;
using System.Collections.Generic;

namespace Rebus.Retry;

/// <summary>
/// Service that is responsible for tracking errors across message deliveries.
/// </summary>
public interface IErrorTracker
{
    /// <summary>
    /// This method is called on each experienced failed delivery. 
    /// </summary>
    void RegisterError(string messageId, Exception exception);
        
    /// <summary>
    /// This method is called when there's no need to track the error anymore
    /// </summary>
    void CleanUp(string messageId);

    /// <summary>
    /// Gets whether the given message ID has had too many error registered for it
    /// </summary>
    bool HasFailedTooManyTimes(string messageId);
        
    /// <summary>
    /// Should get a short error description for the message ID (i.e. something like "5 failed deliveries")
    /// </summary>
    string GetShortErrorDescription(string messageId);
        
    /// <summary>
    /// Should get a full, detailed error description for the message ID (i.e. could be timestamps and full stack traces for all failed deliveries)
    /// </summary>
    string GetFullErrorDescription(string messageId);

    /// <summary>
    /// Gets all caught exceptions for the message ID
    /// </summary>
    IEnumerable<Exception> GetExceptions(string messageId);

    /// <summary>
    /// Marks the given <paramref name="messageId"/> as "FINAL", meaning that it should be considered as "having failed too many times now"
    /// </summary>
    void MarkAsFinal(string messageId);
}