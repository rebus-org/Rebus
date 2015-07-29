using System;
using System.Collections.Generic;
using Rebus.Messages;

namespace Rebus.Bus
{
    /// <summary>
    /// Service that helps re-dispatching deferred messages (i.e. when the payload of the <see cref="TimeoutReply"/> is
    /// deserialized into a message, that message is handled by the re-dispatcher)
    /// </summary>
    interface IHandleDeferredMessage
    {
        void DispatchLocal(object deferredMessage, Guid sagaId, IDictionary<string, object> headers);
        void SendReply(string recipient, TimeoutReply reply, Guid sagaId);
    }
}