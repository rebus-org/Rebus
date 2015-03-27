using Rebus.Messages;

namespace Rebus.Transport.InMem
{
    public static class InMemTransportMessageExtensions
    {
        public static InMemTransportMessage ToInMemTransportMessage(this TransportMessage transportMessage)
        {
            return new InMemTransportMessage(transportMessage);
        }
    }
}