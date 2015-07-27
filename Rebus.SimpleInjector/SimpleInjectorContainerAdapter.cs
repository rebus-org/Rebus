using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport;
using SimpleInjector;
#pragma warning disable 1998

namespace Rebus.SimpleInjector
{
    /// <summary>
    /// Implementation of <see cref="IContainerAdapter"/> that uses Simple Injector to do its thing
    /// </summary>
    public class SimpleInjectorContainerAdapter : IContainerAdapter, IDisposable
    {
        readonly HashSet<IDisposable> _disposables = new HashSet<IDisposable>();
        readonly Container _container;

        /// <summary>
        /// Constructs the container adapter
        /// </summary>
        public SimpleInjectorContainerAdapter(Container container)
        {
            _container = container;
        }

        /// <summary>
        /// Resolves all handlers for the given <typeparamref name="TMessage"/> message type
        /// </summary>
        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            var handlerInstances = _container
                .GetAllInstances<IHandleMessages<TMessage>>()
                .ToList();

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
            _container.RegisterSingle(bus);
            
            _disposables.Add(bus);

            _container.Register(() =>
            {
                var currentMessageContext = MessageContext.Current;
                if (currentMessageContext == null)
                {
                    throw new InvalidOperationException("Attempted to inject the current message context from MessageContext.Current, but it was null! Did you attempt to resolve IMessageContext from outside of a Rebus message handler?");
                }
                return currentMessageContext;
            });
        }

        /// <summary>
        /// Disposes the bus
        /// </summary>
        public void Dispose()
        {
            _disposables.ForEach(d => d.Dispose());
            _disposables.Clear();
        }
    }
}
