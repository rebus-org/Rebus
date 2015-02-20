using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus2.Handlers;

namespace Rebus2.Activation
{
    public class BuiltinHandlerActivator : IHandlerActivator
    {
        readonly List<object> _handlers = new List<object>();

        public IEnumerable<IHandleMessages<TMessage>> GetHandlers<TMessage>()
        {
            return _handlers.OfType<Handler<TMessage>>();
        }

        public BuiltinHandlerActivator Handle<TMessage>(Func<TMessage, Task> handlerFunction)
        {
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