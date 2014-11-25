using EventStore.ClientAPI;

namespace Rebus.EventStore
{
    internal class MessageContainer
    {
        public ReceivedTransportMessage ReceivedTransportMessage { get; set; }
        public ResolvedEvent ResolvedEvent { get; set; }
    }
}