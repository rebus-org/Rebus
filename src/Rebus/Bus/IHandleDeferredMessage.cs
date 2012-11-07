using System;

namespace Rebus.Bus
{
    interface IHandleDeferredMessage
    {
        void Dispatch(object deferredMessage, Guid sagaId);
    }
}