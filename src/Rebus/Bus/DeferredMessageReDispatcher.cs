namespace Rebus.Bus
{
    class DeferredMessageReDispatcher : IHandleDeferredMessage
    {
        readonly IBus bus;

        public DeferredMessageReDispatcher(IBus bus)
        {
            this.bus = bus;
        }

        public void Dispatch(object deferredMessage)
        {
            bus.SendLocal(deferredMessage);
        }
    }
}