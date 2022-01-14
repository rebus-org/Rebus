using System;
using System.Collections.Generic;
using Rebus.Messages;

namespace Rebus.Transport.InMem;

/// <summary>
/// Represents a transport message that was delivered to an in-mem message queue
/// </summary>
public class InMemTransportMessage
{
    readonly DateTimeOffset _creationTime = DateTime.UtcNow;

    /// <summary>
    /// Constructs the in-mem transport message from the given <see cref="TransportMessage"/>
    /// </summary>
    public InMemTransportMessage(TransportMessage transportMessage)
    {
        Headers = transportMessage.Headers;
        Body = transportMessage.Body;
    }

    /// <summary>
    /// Gets the age of this in-mem transport message
    /// </summary>
    public TimeSpan Age => DateTime.UtcNow - _creationTime;

    /// <summary>
    /// Gets the headers of this in-mem transport message
    /// </summary>
    public Dictionary<string,string> Headers { get; }

    /// <summary>
    /// Gets the body data of this in-mem transport message
    /// </summary>
    public byte[] Body { get; }

    /// <summary>
    /// Returns this in-mem transport message's headers and body in a <see cref="TransportMessage"/>
    /// </summary>
    public TransportMessage ToTransportMessage() => new TransportMessage(Headers, Body);
}