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

namespace Rebus.SimpleInjector
{
    public class SimpleInjectorContainerAdapter : IContainerAdapter, IDisposable
    {
        readonly HashSet<IDisposable> _disposables = new HashSet<IDisposable>();
        readonly Container _container;

        public SimpleInjectorContainerAdapter(Container container)
        {
            _container = container;
        }

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

        public void Dispose()
        {
            _disposables.ForEach(d => d.Dispose());
            _disposables.Clear();
        }
    }
}
