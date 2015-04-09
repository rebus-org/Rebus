using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Transport;

namespace Rebus.Activation
{
    /// <summary>
    /// Built-in handler activator that can be used when dependency injection is not required, or when inline
    /// lambda-based handler are wanted
    /// </summary>
    public class BuiltinHandlerActivator : IHandlerActivator
    {
        static ILog _log;

        static BuiltinHandlerActivator()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly List<object> _handlerInstances = new List<object>();
        readonly List<Delegate> _handlerFactories = new List<Delegate>();

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

        public BuiltinHandlerActivator Handle<TMessage>(Func<TMessage, Task> handlerFunction)
        {
            _log.Debug("Adding handler for message type {0}", typeof(TMessage));
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

        public BuiltinHandlerActivator Register<THandler>(Func<THandler> handlerFactory) where THandler : IHandleMessages
        {
            _log.Debug("Adding handler factory for handler type {0}", typeof(THandler));
            _handlerFactories.Add(handlerFactory);
            return this;
        }
    }
}