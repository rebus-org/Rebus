using System;
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

        class HandlerMethodWrapper<T> : IHandleMessages<T>
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
            handlers.Add(new HandlerMethodWrapper<T>(handlerMethod));
            return this;
        }

        public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
        {
            return handlers
                .Where(h => h is IHandleMessages<T>)
                .Cast<IHandleMessages<T>>()
                .ToList();
        }

        public void ReleaseHandlerInstances<T>(IEnumerable<IHandleMessages<T>> handlerInstances)
        {
        }
    }
}