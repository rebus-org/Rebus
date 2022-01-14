using System;

namespace Rebus.Retry.FailFast;

/// <summary>
/// Service to check if a message should fail fast
/// </summary>
public interface IFailFastChecker
{
    /// <summary>
    /// Checks if a message with specific exception should fail fast
    /// </summary>
    bool ShouldFailFast(string messageId, Exception exception);
}