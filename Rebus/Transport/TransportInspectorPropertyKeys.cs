namespace Rebus.Transport;

/// <summary>
/// Enumerates predefined keys which may/may not be used by a <see cref="ITransportInspector"/> implementation to
/// retport back information about the transport
/// </summary>
public static class TransportInspectorPropertyKeys
{
    /// <summary>
    /// Number of messages currently residing in the input queue
    /// </summary>
    public const string QueueLength = "queue-length";
}