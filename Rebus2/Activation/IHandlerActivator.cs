using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus2.Handlers;

namespace Rebus2.Activation
{
    public interface IHandlerActivator
    {
        Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message);
    }
}