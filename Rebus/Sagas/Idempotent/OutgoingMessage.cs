using System.Collections.Generic;
using Rebus.Messages;

namespace Rebus.Sagas.Idempotent;

/// <summary>
/// An outgoing message is a <see cref="Messages.TransportMessage"/> destined for one or more destinations. It is meant to be stored
/// in an <see cref="IdempotencyData"/> instance inside an instance of <see cref="IIdempotentSagaData"/>.
/// </summary>
public class OutgoingMessage
{
    /// <summary>
    /// Constructs the outgoing message destined for the given addresses
    /// </summary>
    public OutgoingMessage(IEnumerable<string> destinationAddresses, TransportMessage transportMessage)
    {
        DestinationAddresses = destinationAddresses;
        TransportMessage = transportMessage;
    }

    /// <summary>
    /// Gets the addresses for which this <see cref="Messages.TransportMessage"/> is supposed to be sent
    /// </summary>
    public IEnumerable<string> DestinationAddresses { get; }
        
    /// <summary>
    /// Gets the transport message
    /// </summary>
    public TransportMessage TransportMessage { get; }
}