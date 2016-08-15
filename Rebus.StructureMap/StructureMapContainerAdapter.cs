using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport;
using StructureMap;
#pragma warning disable 1998

namespace Rebus.StructureMap
{
    /// <summary>
    /// Implementation of <see cref="IContainerAdapter"/> that uses StructureMap to do its thing
    /// </summary>
    public class StructureMapContainerAdapter : IContainerAdapter
    {
        readonly IContainer _container;

        /// <summary>
        /// Constructs the container adapter
        /// </summary>
        public StructureMapContainerAdapter(IContainer container)
        {
            _container = container;
        }

        /// <summary>
        /// Returns all relevant handler instances for the given message
        /// </summary>
        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            var container = transactionContext.GetOrAdd("nested-structuremap-container", () =>
            {
                var nestedContainer = _container.GetNestedContainer();
                transactionContext.OnDisposed(() => nestedContainer.Dispose());
                return nestedContainer;
            });

            return container.GetAllInstances<IHandleMessages<TMessage>>();
        }

        /// <summary>
        /// Sets the bus instance that this <see cref="IContainerAdapter"/> should be able to inject when resolving handler instances
        /// </summary>
        public void SetBus(IBus bus)
        {
            _container.Configure(x =>
            {
                x.For<IBus>().Singleton().Add(bus);
                x.For<IMessageContext>().Transient().Use(() => MessageContext.Current);
            });
        }
    }
}
