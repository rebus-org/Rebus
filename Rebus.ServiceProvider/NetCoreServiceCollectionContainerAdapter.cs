using Rebus.Activation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Transport;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Pipeline;
using Rebus.Extensions;

namespace Rebus.ServiceProvider
{
    /// <summary>
    /// Implementation of <see cref="IContainerAdapter"/> that is backed by an ASP.NET Core Service Provider
    /// </summary>
    /// <seealso cref="Rebus.Activation.IContainerAdapter" />
    public class NetCoreServiceCollectionContainerAdapter : IContainerAdapter
    {
        readonly IServiceCollection _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetCoreServiceCollectionContainerAdapter"/> class.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public NetCoreServiceCollectionContainerAdapter(IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            _services = services;
        }

        /// <summary>
        /// Resolves all handlers for the given <typeparamref name="TMessage"/> message type
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="message"></param>
        /// <param name="transactionContext"></param>
        /// <returns></returns>
        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            var resolvedHandlerInstances = GetAllHandlersInstances<TMessage>();

            transactionContext.OnDisposed(() =>
            {
                foreach (var disposableInstance in resolvedHandlerInstances.OfType<IDisposable>())
                {
                    disposableInstance.Dispose();
                }
            });

            return resolvedHandlerInstances;
        }

        /// <summary>
        /// Sets the bus instance that this <see cref="T:Rebus.Activation.IContainerAdapter" /> should be able to inject when resolving handler instances
        /// </summary>
        /// <param name="bus"></param>
        public void SetBus(IBus bus)
        {
            _services.AddSingleton<IBus>(bus);
            _services.AddTransient<IMessageContext>((s) => MessageContext.Current);
        }

        List<IHandleMessages<TMessage>> GetAllHandlersInstances<TMessage>()
        {
            var container = _services.BuildServiceProvider();

            var handledMessageTypes = typeof(TMessage).GetBaseTypes()
                .Concat(new[] { typeof(TMessage) });

            return handledMessageTypes
                .SelectMany(t =>
                {
                    var implementedInterface = typeof(IHandleMessages<>).MakeGenericType(t);

                    return container.GetServices(implementedInterface).Cast<IHandleMessages>();
                })
                .Cast<IHandleMessages<TMessage>>()
                .ToList();
        }
    }
}
