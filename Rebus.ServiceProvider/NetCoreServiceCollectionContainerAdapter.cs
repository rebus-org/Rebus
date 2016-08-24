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
    public class NetCoreServiceCollectionContainerAdapter : IContainerAdapter
    {
        readonly IServiceCollection _services;

        public NetCoreServiceCollectionContainerAdapter(IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            _services = services;
        }

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
