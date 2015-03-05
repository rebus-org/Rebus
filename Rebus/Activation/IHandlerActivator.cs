using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Handlers;

namespace Rebus.Activation
{
    public interface IHandlerActivator
    {
        Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message);
    }
}