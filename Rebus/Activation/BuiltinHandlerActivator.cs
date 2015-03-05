using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Handlers;
using Rebus.Logging;

namespace Rebus.Activation
{
    public class BuiltinHandlerActivator : IHandlerActivator
    {
        static ILog _log;

        static BuiltinHandlerActivator()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly List<object> _handlerInstances = new List<object>();
        readonly List<Delegate> _handlerFactories = new List<Delegate>();

        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message)
        {
            var factories = _handlerFactories.OfType<Func<IHandleMessages<TMessage>>>();
            var handlers = factories.Select(factory => factory());
            return _handlerInstances.OfType<Handler<TMessage>>().Concat(handlers);
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

            public Task Handle(TMessage message)
            {
                return _handlerFunction(message);
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