using System;

namespace Rebus.Retry;

/// <summary>
/// Responsible for logging exceptions caught in handlers
/// </summary>
public interface IExceptionLogger
{
    /// <summary>
    /// Logs the exception in a nice way
    /// </summary>
    void LogException(string messageId, Exception exception, int errorCount, bool isFinal);
}