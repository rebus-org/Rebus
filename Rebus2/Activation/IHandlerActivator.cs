using System.Collections.Generic;
using Rebus2.Handlers;

namespace Rebus2.Activation
{
    public interface IHandlerActivator
    {
        IEnumerable<IHandleMessages<TMessage>> GetHandlers<TMessage>();
    }
}