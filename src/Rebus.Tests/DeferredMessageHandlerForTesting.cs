using System;
using Rebus.Bus;
using Rebus.Messages;

namespace Rebus.Tests
{
    class DeferredMessageHandlerForTesting : IHandleDeferredMessage
    {
        public void DispatchLocal(object deferredMessage, Guid sagaId)
        {
        }

        public void SendReply(string recipient, TimeoutReply reply, Guid sagaId)
        {
        }
    }
}