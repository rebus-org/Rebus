using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Handlers;
using Rebus.Transport;

namespace Rebus.Activation;

sealed class EmptyActivator : IHandlerActivator
{
    public Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext) =>
        Task.FromResult(Enumerable.Empty<IHandleMessages<TMessage>>());
}