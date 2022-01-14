using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Handlers;
using Rebus.Transport;

namespace Rebus.Activation;

/// <summary>
/// Responsible for creating handlers for a given message type
/// </summary>
public interface IHandlerActivator
{
    /// <summary>
    /// Must return all relevant handler instances for the given message
    /// </summary>
    Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext);
}