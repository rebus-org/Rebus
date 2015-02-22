using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus2.Handlers;
using Rebus2.Logging;

namespace Rebus2.Activation
{
    public class BuiltinHandlerActivator : IHandlerActivator
    {
        static ILog _log;

        static BuiltinHandlerActivator()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly List<object> _handlers = new List<object>();

        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message)
        {
            return _handlers.OfType<Handler<TMessage>>();
        }

        public BuiltinHandlerActivator Handle<TMessage>(Func<TMessage, Task> handlerFunction)
        {
            _log.Debug("Adding handler for {0}", typeof(TMessage));
            _handlers.Add(new Handler<TMessage>(handlerFunction));
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
    }
}