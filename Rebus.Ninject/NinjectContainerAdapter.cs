using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ninject;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport;
#pragma warning disable 1998

namespace Rebus.Ninject
{
    /// <summary>
    /// Implementation of <see cref="IContainerAdapter"/> that uses Ninject to do its thing
    /// </summary>
    public class NinjectContainerAdapter : IContainerAdapter
    {
        readonly IKernel _kernel;

        /// <summary>
        /// Constructs the adapter
        /// </summary>
        public NinjectContainerAdapter(IKernel kernel)
        {
            _kernel = kernel;
        }

        /// <summary>
        /// Resolves all handlers for the given <typeparamref name="TMessage"/> message type
        /// </summary>
        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            var handlerInstances = GetAllHandlerInstances<TMessage>();

            transactionContext.OnDisposed(() =>
            {
                foreach (var disposableInstance in handlerInstances.OfType<IDisposable>())
                {
                    disposableInstance.Dispose();
                }
            });

            return handlerInstances;
        }

        /// <summary>
        /// Stores the bus instance
        /// </summary>
        public void SetBus(IBus bus)
        {
            _kernel.Bind<IBus>().ToConstant(bus);

            _kernel.Bind<IMessageContext>().ToMethod(c =>
            {
                var currentMessageContext = MessageContext.Current;
                if (currentMessageContext == null)
                {
                    throw new InvalidOperationException(
                        "Attempted to inject the current message context from MessageContext.Current, but it was null! Did you attempt to resolve IMessageContext from outside of a Rebus message handler?");
                }
                return currentMessageContext;
            });

            _kernel.Get<IBus>();
        }

        IEnumerable<IHandleMessages<TMessage>> GetAllHandlerInstances<TMessage>()
        {
            var handledMessageTypes = typeof(TMessage).GetBaseTypes()
                .Concat(new[] { typeof(TMessage) });

            return handledMessageTypes
                .SelectMany(handledMessageType =>
                {
                    var implementedInterface = typeof(IHandleMessages<>).MakeGenericType(handledMessageType);

                    return _kernel.GetAll(implementedInterface).Cast<IHandleMessages>();
                })
                .Cast<IHandleMessages<TMessage>>();
        }
    }
}
