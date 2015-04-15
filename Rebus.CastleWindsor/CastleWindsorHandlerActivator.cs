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
using Rebus.Transport;

namespace Rebus.CastleWindsor
{
    public class CastleWindsorHandlerActivator : IContainerAdapter
    {
        readonly IWindsorContainer _windsorContainer;

        public CastleWindsorHandlerActivator(IWindsorContainer windsorContainer)
        {
            _windsorContainer = windsorContainer;
        }

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

        public void SetBus(IBus bus)
        {
            _windsorContainer.Register(
                Component.For<IBus>().Instance(bus),
                Component.For<InstanceDisposer>()
                );

            _windsorContainer.Resolve<InstanceDisposer>();
        }

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

        IEnumerable<IHandleMessages<TMessage>> GetAllHandlerInstances<TMessage>()
        {
            var handledMessageTypes = typeof(TMessage).GetBaseTypes()
                .Concat(new[]{typeof(TMessage)});

            return handledMessageTypes
                .SelectMany(handledMessageType =>
                {
                    var implementedInterface = typeof (IHandleMessages<>).MakeGenericType(handledMessageType);

                    return _windsorContainer.ResolveAll(implementedInterface).Cast<IHandleMessages>();
                })
                .Cast<IHandleMessages<TMessage>>();
        }
    }
}
