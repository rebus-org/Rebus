using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Handlers;

namespace Rebus.Activation
{
    /// <summary>
    /// Responsible for creating handlers for a given message type
    /// </summary>
    public interface IHandlerActivator
    {
        Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message);
    }
}