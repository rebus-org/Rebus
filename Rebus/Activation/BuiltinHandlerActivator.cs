using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Transport;
#pragma warning disable 1998

namespace Rebus.Activation
{
    /// <summary>
    /// Built-in handler activator that can be used when dependency injection is not required, or when inline
    /// lambda-based handler are wanted
    /// </summary>
    public class BuiltinHandlerActivator : IContainerAdapter, IDisposable
    {
        readonly List<object> _handlerInstances = new List<object>();
        readonly List<Delegate> _handlerFactories = new List<Delegate>();

        /// <summary>
        /// Returns all relevant handler instances for the given message by looking up compatible registered functions and instance factory methods.
        /// </summary>
        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            var factories = _handlerFactories.OfType<Func<IHandleMessages<TMessage>>>();
            var instancesFromFactories = factories.Select(factory => factory());
            var instancesJustInstances = _handlerInstances.OfType<IHandleMessages<TMessage>>();

            var handlerInstances = instancesJustInstances.Concat(instancesFromFactories).ToList();

            transactionContext.OnDisposed(() =>
            {
                handlerInstances
                    .OfType<IDisposable>()
                    .ForEach(i => i.Dispose());
            });

            return handlerInstances;
        }

        /// <summary>
        /// Gets the bus instance that this activator was configured with
        /// </summary>
        public IBus Bus { get; private set; }

        /// <summary>
        /// Stores the bus instance
        /// </summary>
        public void SetBus(IBus bus)
        {
            if (bus == null)
            {
                throw new ArgumentNullException("bus", "You need to provide a bus instance in order to call this method!");
            }
            if (Bus != null)
            {
                throw new InvalidOperationException(string.Format("Cannot set bus to {0} because it has already been set to {1}",
                bus, Bus));
            }
            Bus = bus;
        }

        /// <summary>
        /// Sets up an inline handler for messages of type <see cref="TMessage"/>
        /// </summary>
        public BuiltinHandlerActivator Handle<TMessage>(Func<TMessage, Task> handlerFunction)
        {
            _handlerInstances.Add(new Handler<TMessage>(handlerFunction));
            return this;
        }

        class Handler<TMessage> : IHandleMessages<TMessage>
        {
            readonly Func<TMessage, Task> _handlerFunction;

            public Handler(Func<TMessage, Task> handlerFunction)
            {
                _handlerFunction = handlerFunction;
            }

            public async Task Handle(TMessage message)
            {
                await _handlerFunction(message);
            }
        }

        /// <summary>
        /// Registers the given factory method as a handler factory method for messages of the types determined by which
        /// <see cref="IHandleMessages{TMessage}"/> interfaces are implemeted.
        /// </summary>
        public BuiltinHandlerActivator Register<THandler>(Func<THandler> handlerFactory) where THandler : IHandleMessages
        {
            _handlerFactories.Add(handlerFactory);
            return this;
        }

        public void Dispose()
        {
            if (Bus == null) return;
            Bus.Dispose();
        }
    }
}