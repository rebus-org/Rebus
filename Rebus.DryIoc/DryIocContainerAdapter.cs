using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DryIoc;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport;

#pragma warning disable 1998

namespace Rebus.DryIoc
{
    public class DryIocContainerAdapter : IContainerAdapter
    {
        readonly IContainer _container;

        /// <summary>
        /// Constructs the adapter, using the specified container
        /// </summary>
        public DryIocContainerAdapter(IContainer container)
        {
            _container = container;
            _container.Register(Made.Of(() => GetCurrentMessageContext()));
        }

        /// <summary>
        /// Resolves all handlers for the given <typeparamref name="TMessage"/> message type
        /// </summary>
        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            var handlerInstances = _container.Resolve<IList<IHandleMessages<TMessage>>>();

            transactionContext.OnDisposed(() =>
            {
                handlerInstances
                    .OfType<IDisposable>()
                    .ForEach(disposable =>
                    {
                        disposable.Dispose();
                    });
            });

            return handlerInstances;
        }

        /// <summary>
        /// Stores the bus instance
        /// </summary>
        public void SetBus(IBus bus)
        {
            _container.RegisterInstance(bus);
        }

        /// <summary>
        /// Returns the current message context and ensures it is not null
        /// </summary>
        /// <returns>IMessageContext</returns>
        internal static IMessageContext GetCurrentMessageContext()
        {
            var currentMessageContext = MessageContext.Current;
            if (currentMessageContext == null)
            {
                throw new InvalidOperationException("Attempted to inject the current message context from MessageContext.Current, but it was null! Did you attempt to resolve IMessageContext from outside of a Rebus message handler?");
            }
            return currentMessageContext;
        }
    }
}
