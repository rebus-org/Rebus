using System;
using Rebus.Bus;

namespace Rebus.Tests
{
    class DeferredMessageHandlerForTesting : IHandleDeferredMessage
    {
        public void Dispatch(object deferredMessage, Guid sagaId)
        {
        }
    }
}