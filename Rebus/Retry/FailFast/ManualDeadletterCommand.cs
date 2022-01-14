namespace Rebus.Retry.FailFast;

class ManualDeadletterCommand
{
    public string ErrorDetails { get; }

    public ManualDeadletterCommand(string errorDetails)
    {
        ErrorDetails = errorDetails;
    }
}