using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Transport;
using SimpleInjector;

namespace Rebus.SimpleInjector
{
    public class SimpleInjectorContainerAdapter : IContainerAdapter
    {
        readonly Container _container;

        public SimpleInjectorContainerAdapter(Container container)
        {
            _container = container;
        }

        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {

            return Enumerable.Empty<IHandleMessages<TMessage>>();
        }

        public void SetBus(IBus bus)
        {
        }
    }
}
