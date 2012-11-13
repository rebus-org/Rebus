using System;
using Rebus.Shared;

namespace Rebus.Bus
{
    class DeferredMessageReDispatcher : IHandleDeferredMessage
    {
        readonly IBus bus;

        public DeferredMessageReDispatcher(IBus bus)
        {
            this.bus = bus;
        }

        public void Dispatch(object deferredMessage, Guid sagaId)
        {
            if (sagaId != Guid.Empty)
            {
                bus.AttachHeader(deferredMessage, Headers.AutoCorrelationSagaId, sagaId.ToString());
            }

            bus.SendLocal(deferredMessage);
        }
    }
}