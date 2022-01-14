using Rebus.Messages;

namespace Rebus.Transport.InMem;

/// <summary>
/// Extensions that make it nice to work with <see cref="InMemTransportMessage"/> and <see cref="TransportMessage"/>
/// </summary>
public static class InMemTransportMessageExtensions
{
    /// <summary>
    /// Returns a new <see cref="InMemTransportMessage"/> containing the headers and the body data of the <see cref="TransportMessage"/>
    /// </summary>
    public static InMemTransportMessage ToInMemTransportMessage(this TransportMessage transportMessage)
    {
        return new InMemTransportMessage(transportMessage);
    }
}