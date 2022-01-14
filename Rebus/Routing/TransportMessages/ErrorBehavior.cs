namespace Rebus.Routing.TransportMessages;

/// <summary>
/// Options on how to handle exceptions when attempting to forward transport messages
/// </summary>
public enum ErrorBehavior
{
    /// <summary>
    /// Indicates that no error handling should be done. This puts the burden of handling errors into the hands
    /// of the implementor of the transport message forwarding function, and thus it should handle errors by
    /// forwarding the message somewhere else
    /// </summary>
    RetryForever,

    /// <summary>
    /// Indicates that the transport message should be forwarded to the error queue in the event that there is an error.
    /// This is done in a "fail fast"-fashion, so there will be no additional delivery attempts.
    /// </summary>
    ForwardToErrorQueue
}