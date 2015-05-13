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

namespace Rebus.Ninject
{
    public class NinjectContainerAdapter : IContainerAdapter
    {
        readonly IKernel _kernel;

        public NinjectContainerAdapter(IKernel kernel)
        {
            _kernel = kernel;
        }

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
