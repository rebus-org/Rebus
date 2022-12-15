using System;

namespace Rebus.Retry.FailFast;

class ManualDeadletterCommand
{
    public Exception Exception { get; }

    public ManualDeadletterCommand(Exception exception)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }
}