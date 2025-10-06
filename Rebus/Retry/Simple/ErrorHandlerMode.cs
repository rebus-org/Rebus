namespace Rebus.Retry.Simple;

/// <summary>
/// Enumerates available modes for when the <see cref="IErrorHandler"/> gets called.
/// </summary>
public enum ErrorHandlerMode
{
    /// <summary>
    /// Default is immediately after having processed a message and caught an exception that causes the <see cref="IErrorTracker"/> to
    /// report that the message has failed too many times. This mode is lightweight, because it doesn't do any error checks in the sunshine
    /// scenario. However, it doesn't work when both the transport and the work is being done in the same SQL transaction, because it will
    /// not be able to roll back the work and still send the message to the error queue.
    /// </summary>
    Immediately,

    /// <summary>
    /// "Before dispatch" means that caught exceptions are registered in the error tracker when caught, and then the poison message will be
    /// deadlettered on the subsequent consumption attempt. This works with transports that enlist in the work transaction, because no work
    /// is being done when a message is deadlettered.
    /// </summary>
    NextDelivery
}