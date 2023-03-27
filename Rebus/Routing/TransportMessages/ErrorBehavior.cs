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
    /// Indicates that the transport message should be passed to the error handler when there is an error, which
    /// usually means that the message is moved to the error queue.
    /// </summary>
    Normal
}