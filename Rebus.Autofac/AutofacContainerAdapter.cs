using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport;
#pragma warning disable 1998

namespace Rebus.Autofac
{
    /// <summary>
    /// Implementation of <see cref="IContainerAdapter"/> that is backed by an Autofac container
    /// </summary>
    public class AutofacContainerAdapter : IContainerAdapter
    {
        readonly IContainer _container;
        private readonly ILifetimeScope _scope;
        private readonly object _key;

        /// <summary>
        /// Constructs the adapter, using the specified container
        /// </summary>
        public AutofacContainerAdapter(IContainer container, object key = null)
        {
            _container = container;
            _key = key;
        }

        /// <summary>
        /// Constructs the adapter, using the specified lifetime scope
        /// </summary>
        public AutofacContainerAdapter(ILifetimeScope scope, object key = null)
        {
            _scope = scope;
            _key = key;
        }

        /// <summary>
        /// Resolves all handlers for the given <typeparamref name="TMessage"/> message type
        /// </summary>
        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            var lifetimeScope = transactionContext
                .GetOrAdd("current-autofac-lifetime-scope", () =>
                {
                    var scope = (_container ?? _scope).BeginLifetimeScope();

                    transactionContext.OnDisposed(() => scope.Dispose());

                    return scope;
                });

            var handledMessageTypes = typeof (TMessage).GetBaseTypes()
                .Concat(new[] {typeof (TMessage)});

            return handledMessageTypes
                .SelectMany(handledMessageType =>
                {
                    var implementedInterface = typeof(IHandleMessages<>).MakeGenericType(handledMessageType);
                    var implementedInterfaceSequence = typeof(IEnumerable<>).MakeGenericType(implementedInterface);

                    object instances;
                    if (_key == null)
                    {
                        if (lifetimeScope.TryResolve(implementedInterfaceSequence, out instances))
                        return (IEnumerable<IHandleMessages>)instances;
                    }
                    else
                    {
                        if (lifetimeScope.TryResolveKeyed(_key, implementedInterfaceSequence, out instances))
                        return (IEnumerable<IHandleMessages>) instances;
                    }

                    throw new Exception($"Could not resolve any implementations of IHandlerMessages<{_key ?? typeof (TMessage).Name}>.");
                })
                .Cast<IHandleMessages<TMessage>>();
        }

        /// <summary>
        /// Stores the bus instance
        /// </summary>
        public void SetBus(IBus bus)
        {
            if (_container == null)
                return;

            var containerBuilder = new ContainerBuilder();

            if (_key == null)
            {
                containerBuilder
                    .RegisterInstance(bus)
                    .SingleInstance();
            }
            else
            {
                containerBuilder
                    .RegisterInstance(bus)
                    .SingleInstance()
                    .Keyed<IBus>(_key);
            }

            containerBuilder.RegisterRebus();

            containerBuilder.Update(_container);
        }
    }
}
