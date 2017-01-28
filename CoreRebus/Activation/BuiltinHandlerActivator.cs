using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Pipeline;
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
        readonly List<Delegate> _handlerFactoriesNoArguments = new List<Delegate>();
        readonly List<Delegate> _handlerFactoriesMessageContextArgument = new List<Delegate>();
        readonly List<Delegate> _handlerFactoriesBusAndMessageContextArguments = new List<Delegate>();

        /// <summary>
        /// Returns all relevant handler instances for the given message by looking up compatible registered functions and instance factory methods.
        /// </summary>
        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (transactionContext == null) throw new ArgumentNullException(nameof(transactionContext));

            var instancesFromNoArgumentFactories = _handlerFactoriesNoArguments
                .OfType<Func<IHandleMessages<TMessage>>>().Select(factory => factory());

            var instancesFromMessageContextArgumentFactories = _handlerFactoriesMessageContextArgument
                .OfType<Func<IMessageContext, IHandleMessages<TMessage>>>().Select(factory =>
                {
                    var messageContext = MessageContext.Current;
                    if (messageContext == null)
                    {
                        throw new InvalidOperationException("Attempted to resolve handler with message context, but no current context could be found on MessageContext.Current");
                    }
                    return factory(messageContext);
                });

            var instancesFromBusAndMessageContextArgumentFactories = _handlerFactoriesBusAndMessageContextArguments
                .OfType<Func<IBus, IMessageContext, IHandleMessages<TMessage>>>().Select(factory =>
                {
                    var messageContext = MessageContext.Current;
                    if (messageContext == null)
                    {
                        throw new InvalidOperationException("Attempted to resolve handler with message context, but no current context could be found on MessageContext.Current");
                    }
                    return factory(Bus, messageContext);
                });

            var instancesJustInstances = _handlerInstances.OfType<IHandleMessages<TMessage>>();

            var handlerInstances = instancesJustInstances
                .Concat(instancesFromNoArgumentFactories)
                .Concat(instancesFromMessageContextArgumentFactories)
                .Concat(instancesFromBusAndMessageContextArgumentFactories)
                .ToList();

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
                throw new ArgumentNullException(nameof(bus), "You need to provide a bus instance in order to call this method!");
            }
            if (Bus != null)
            {
                throw new InvalidOperationException($"Cannot set bus to {bus} because it has already been set to {Bus}");
            }

            Bus = bus;
        }

        /// <summary>
        /// Sets up an inline handler for messages of type <typeparamref name="TMessage"/> with the <see cref="IBus"/> and the current <see cref="IMessageContext"/> available
        /// </summary>
        public BuiltinHandlerActivator Handle<TMessage>(Func<IBus, IMessageContext, TMessage, Task> handlerFunction)
        {
            _handlerInstances.Add(new Handler<TMessage>((bus, message) => handlerFunction(bus, MessageContext.Current, message), () => Bus));
            return this;
        }

        /// <summary>
        /// Sets up an inline handler for messages of type <typeparamref name="TMessage"/> with the <see cref="IBus"/> available
        /// </summary>
        public BuiltinHandlerActivator Handle<TMessage>(Func<IBus, TMessage, Task> handlerFunction)
        {
            _handlerInstances.Add(new Handler<TMessage>(handlerFunction, () => Bus));
            return this;
        }

        /// <summary>
        /// Sets up an inline handler for messages of type <typeparamref name="TMessage"/>
        /// </summary>
        public BuiltinHandlerActivator Handle<TMessage>(Func<TMessage, Task> handlerFunction)
        {
            _handlerInstances.Add(new Handler<TMessage>((bus, message) => handlerFunction(message), () => Bus));
            return this;
        }

        class Handler<TMessage> : IHandleMessages<TMessage>
        {
            readonly Func<IBus, TMessage, Task> _handlerFunction;
            readonly Func<IBus> _getBus;

            public Handler(Func<IBus, TMessage, Task> handlerFunction, Func<IBus> getBus)
            {
                _handlerFunction = handlerFunction;
                _getBus = getBus; // store this function here because of Hen&Egg-Problem between handler activator and bus
            }

            public async Task Handle(TMessage message)
            {
                await _handlerFunction(_getBus(), message);
            }
        }

        /// <summary>
        /// Registers the given factory method as a handler factory method for messages of the types determined by which
        /// <see cref="IHandleMessages{TMessage}"/> interfaces are implemeted.
        /// </summary>
        public BuiltinHandlerActivator Register<THandler>(Func<THandler> handlerFactory) where THandler : IHandleMessages
        {
            _handlerFactoriesNoArguments.Add(handlerFactory);
            return this;
        }

        /// <summary>
        /// Registers the given factory method as a handler factory method for messages of the types determined by which
        /// <see cref="IHandleMessages{TMessage}"/> interfaces are implemeted.
        /// </summary>
        public BuiltinHandlerActivator Register<THandler>(Func<IMessageContext, THandler> handlerFactory) where THandler : IHandleMessages
        {
            _handlerFactoriesMessageContextArgument.Add(handlerFactory);
            return this;
        }

        /// <summary>
        /// Registers the given factory method as a handler factory method for messages of the types determined by which
        /// <see cref="IHandleMessages{TMessage}"/> interfaces are implemeted.
        /// </summary>
        public BuiltinHandlerActivator Register<THandler>(Func<IBus, IMessageContext, THandler> handlerFactory) where THandler : IHandleMessages
        {
            _handlerFactoriesBusAndMessageContextArguments.Add(handlerFactory);
            return this;
        }

        /// <summary>
        /// Disposes the contained bus instance
        /// </summary>
        public void Dispose()
        {
            if (Bus == null) return;

            var disposable = Bus;
            try
            {
                disposable.Dispose();
            }
            finally
            {
                Bus = null;
            }
        }
    }
}