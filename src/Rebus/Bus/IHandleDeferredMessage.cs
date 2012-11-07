using System;
using Rebus.Messages;

namespace Rebus.Bus
{
    /// <summary>
    /// Service that helps re-dispatching deferred messages (i.e. when the
    /// payload of the <see cref="TimeoutReply"/> is deserialized into a message,
    /// that message is handled by the re-dispatcher)
    /// </summary>
    interface IHandleDeferredMessage
    {
        void Dispatch(object deferredMessage, Guid sagaId);
    }
}