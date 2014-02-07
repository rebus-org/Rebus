using System;
using Rebus.Messages;
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

        public void DispatchLocal(object deferredMessage, Guid sagaId)
        {
            if (sagaId != Guid.Empty)
            {
                bus.AttachHeader(deferredMessage, Headers.AutoCorrelationSagaId, sagaId.ToString());
            }

            bus.SendLocal(deferredMessage);
        }

        public void SendReply(string recipient, TimeoutReply reply, Guid sagaId)
        {
            if (sagaId != Guid.Empty)
            {
                bus.AttachHeader(reply, Headers.AutoCorrelationSagaId, sagaId.ToString());
            }

            bus.Advanced.Routing.Send(recipient, reply);
        }
    }
}