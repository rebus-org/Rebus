using System;

namespace Rebus.Retry.FailFast;

/// <summary>
/// Object that Rebus can add to its step context to signal to the retry step that it should immediately dead-letter the message being handled
/// </summary>
public class ManualDeadletterCommand
{
    /// <summary>
    /// Gets the exception passed via this command
    /// </summary>
    public Exception Exception { get; }

    internal ManualDeadletterCommand(Exception exception)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }
}