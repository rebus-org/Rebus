using Rebus.Messages;

namespace Rebus.Transport;

/// <summary>
/// Represents one single transport messages for one particular destination
/// </summary>
public class OutgoingTransportMessage
{
    /// <summary>
    /// Gets the transport message
    /// </summary>
    public TransportMessage TransportMessage { get; }

    /// <summary>
    /// Gets the destination address
    /// </summary>
    public string DestinationAddress { get; }

    /// <summary>
    /// Constructs the outgoing message
    /// </summary>
    public OutgoingTransportMessage(TransportMessage transportMessage, string destinationAddress)
    {
        TransportMessage = transportMessage;
        DestinationAddress = destinationAddress;
    }
}