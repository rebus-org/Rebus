using System;
using System.Collections.Generic;
using Rebus.Bus;
using Rebus.Messages;

namespace Rebus.Tests
{
    class DeferredMessageHandlerForTesting : IHandleDeferredMessage
    {
        public void DispatchLocal(object deferredMessage, Guid sagaId, IDictionary<string, object> headers)
        {
        }

        public void SendReply(string recipient, TimeoutReply reply, Guid sagaId)
        {
        }
    }
}