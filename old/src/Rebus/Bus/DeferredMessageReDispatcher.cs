using System;
using System.Collections.Generic;
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

        public void DispatchLocal(object deferredMessage, Guid sagaId, IDictionary<string, object> headers)
        {
            if (sagaId != Guid.Empty)
            {
                bus.AttachHeader(deferredMessage, Headers.AutoCorrelationSagaId, sagaId.ToString());
            }

            foreach (var header in headers)
            {
                bus.AttachHeader(deferredMessage, header.Key, (header.Value ?? "").ToString());
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