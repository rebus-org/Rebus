using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Tests
{
    /// <summary>
    /// Handler factory that allows lambdas to be registered as message handlers.
    /// </summary>
    public class HandlerActivatorForTesting : IActivateHandlers
    {
        readonly List<object> handlers = new List<object>();

        public class HandlerMethodWrapper<T> : IHandleMessages<T>
        {
            readonly Action<T> action;

            public HandlerMethodWrapper(Action<T> action)
            {
                this.action = action;
            }

            public void Handle(T message)
            {
                action(message);
            }
        }

        public HandlerActivatorForTesting Handle<T>(Action<T> handlerMethod)
        {
            return UseHandler(new HandlerMethodWrapper<T>(handlerMethod));
        }

        public HandlerActivatorForTesting UseHandler(object handler)
        {
            handlers.Add(handler);
            return this;
        }

        public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
        {
            var handlerInstances = handlers
                .Where(h => h.GetType().GetInterfaces().Any(i => i == typeof(IHandleMessages<T>)))
                .Cast<IHandleMessages<T>>()
                .ToList();

            return handlerInstances;
        }

        public IEnumerable<IMessageModule> GetMessageModules()
        {
            return new IMessageModule[0];
        }

        public void Release(IEnumerable handlerInstances)
        {
        }
    }
}