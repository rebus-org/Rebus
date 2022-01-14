using System;

namespace Rebus.Messages;

/// <summary>
/// Wraps the originally received transport message while processing the message because the original 
/// instance might be replaced during the processing (e.g. when its body is changed during decompression/decryption etc.)
/// This instance must not be changed
/// </summary>
public class OriginalTransportMessage
{
    /// <summary>
    /// Gets the originally received transport message
    /// </summary>
    public TransportMessage TransportMessage { get; }

    /// <summary>
    /// Creates the wrapper
    /// </summary>
    public OriginalTransportMessage(TransportMessage transportMessage)
    {
        TransportMessage = transportMessage ?? throw new ArgumentNullException(nameof(transportMessage));
    }
}