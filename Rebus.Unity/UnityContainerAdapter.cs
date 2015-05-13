using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.Unity
{
    public class UnityContainerAdapter : IContainerAdapter
    {
        readonly IUnityContainer _unityContainer;

        public UnityContainerAdapter(IUnityContainer unityContainer)
        {
            _unityContainer = unityContainer;
        }

        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            var resolvedHandlerInstances = ResolvePoly<TMessage>();
            
            transactionContext.OnDisposed(() =>
            {
                foreach (var disposableInstance in resolvedHandlerInstances.OfType<IDisposable>())
                {
                    disposableInstance.Dispose();
                }
            });
            
            return resolvedHandlerInstances;
        }

        IEnumerable<IHandleMessages<TMessage>> ResolvePoly<TMessage>()
        {
            var handledMessageTypes = typeof(TMessage).GetBaseTypes()
                .Concat(new[] { typeof(TMessage) });

            return handledMessageTypes
                .SelectMany(handledMessageType =>
                {
                    var implementedInterface = typeof (IHandleMessages<>).MakeGenericType(handledMessageType);

                    return _unityContainer.ResolveAll(implementedInterface).Cast<IHandleMessages>();
                })
                .Cast<IHandleMessages<TMessage>>();
        }

        public void SetBus(IBus bus)
        {
            _unityContainer.RegisterInstance(bus, new ContainerControlledLifetimeManager());

            _unityContainer.RegisterType<IMessageContext>(new InjectionFactory(c =>
            {
                var currentMessageContext = MessageContext.Current;
                if (currentMessageContext == null)
                {
                    throw new InvalidOperationException("Attempted to inject the current message context from MessageContext.Current, but it was null! Did you attempt to resolve IMessageContext from outside of a Rebus message handler?");
                }
                return currentMessageContext;
            }));
        }
    }
}
