using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport;
// ReSharper disable ClassNeverInstantiated.Local
#pragma warning disable 1998

namespace Rebus.CastleWindsor
{
    /// <summary>
    /// Implementation of <see cref="IContainerAdapter"/> that is backed by a Windsor Container
    /// </summary>
    public class CastleWindsorContainerAdapter : IContainerAdapter
    {
        readonly IWindsorContainer _windsorContainer;

        /// <summary>
        /// Constructs the Windsor handler activator
        /// </summary>
        public CastleWindsorContainerAdapter(IWindsorContainer windsorContainer)
        {
            if (windsorContainer == null) throw new ArgumentNullException(nameof(windsorContainer));
            _windsorContainer = windsorContainer;
        }

        /// <summary>
        /// Resolves all handlers for the given <typeparamref name="TMessage"/> message type
        /// </summary>
        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            var handlerInstances = GetAllHandlerInstances<TMessage>();

            transactionContext.OnDisposed(() =>
            {
                foreach (var instance in handlerInstances)
                {
                    _windsorContainer.Release(instance);
                }
            });

            return handlerInstances;
        }

        /// <summary>
        /// Stores the bus instance
        /// </summary>
        public void SetBus(IBus bus)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus), "You need to provide a bus instance in order to call this method!");

            _windsorContainer
                .Register(
                    Component.For<IBus>().Instance(bus).LifestyleSingleton(),
                    Component.For<InstanceDisposer>(),
                  
                    Component.For<IMessageContext>()
                        .UsingFactoryMethod(k =>
                        {
                            var currentMessageContext = MessageContext.Current;
                            if (currentMessageContext == null)
                            {
                                throw new InvalidOperationException("Attempted to inject the current message context from MessageContext.Current, but it was null! Did you attempt to resolve IMessageContext from outside of a Rebus message handler?");
                            }
                            return currentMessageContext;
                        }, managedExternally: true)
                        .LifestyleTransient()
                );

            _windsorContainer.Resolve<InstanceDisposer>();
        }

        /// <summary>
        /// containehack to makes sure we dispose the bus instance when the container is disposed
        /// </summary>
        class InstanceDisposer : IDisposable
        {
            readonly IBus _bus;

            public InstanceDisposer(IBus bus)
            {
                _bus = bus;
            }

            public void Dispose()
            {
                _bus.Dispose();
            }
        }

        List<IHandleMessages<TMessage>> GetAllHandlerInstances<TMessage>()
        {
            var handledMessageTypes = typeof(TMessage).GetBaseTypes()
                .Concat(new[]{typeof(TMessage)});

            return handledMessageTypes
                .SelectMany(handledMessageType =>
                {
                    var implementedInterface = typeof (IHandleMessages<>).MakeGenericType(handledMessageType);

                    return _windsorContainer.ResolveAll(implementedInterface).Cast<IHandleMessages>();
                })
                .Cast<IHandleMessages<TMessage>>()
                .ToList();
        }
    }
}
