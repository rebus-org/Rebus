using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport;
using StructureMap;

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

        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            return _container.GetAllInstances<IHandleMessages<TMessage>>();
        }

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
