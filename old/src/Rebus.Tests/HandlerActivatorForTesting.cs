using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Tests
{
    /// <summary>
    ///   Handler factory that allows lambdas to be registered as message handlers.
    /// </summary>
    public class HandlerActivatorForTesting : IActivateHandlers
    {
        private readonly Dictionary<Type, List<Func<IHandleMessages>>> handlers =
            new Dictionary<Type, List<Func<IHandleMessages>>>();

        public IEnumerable<IHandleMessages> GetHandlerInstancesFor<T>()
        {
            return (from handler in handlers
                    where handler.Key == typeof (T)
                    from activator in handler.Value
                    select (IHandleMessages<T>) activator()).ToList();
        }

        public void Release(IEnumerable handlerInstances)
        {
        }

        public HandlerActivatorForTesting Handle<T>(Action<T> handlerMethod)
        {
            return UseHandler(new HandlerMethodWrapper<T>(handlerMethod));
        }

        public HandlerActivatorForTesting UseHandler(IHandleMessages handler)
        {
            return UseHandler(handler.GetType(), () => handler);
        }
        
        public HandlerActivatorForTesting UseHandler<T>(Func<T> handlerActivator) where T : IHandleMessages
        {
            return UseHandler(typeof(T), handlerActivator as Func<IHandleMessages>);
        }

        HandlerActivatorForTesting UseHandler(Type handlerType, Func<IHandleMessages> handlerActivator)
        {
            var messagesTypes = from @interface in handlerType.GetInterfaces()
                                where
                                    @interface.IsGenericType &&
                                    @interface.GetGenericTypeDefinition() == typeof (IHandleMessages<>)
                                select @interface.GetGenericArguments()[0];

            foreach (var messageType in messagesTypes)
            {
                List<Func<IHandleMessages>> funcs;
                if (!handlers.TryGetValue(messageType, out funcs))
                {
                    funcs = new List<Func<IHandleMessages>>();
                    handlers.Add(messageType, funcs);
                }

                funcs.Add(handlerActivator);
            }

            return this;
        }

        public class HandlerMethodWrapper<T> : IHandleMessages<T>
        {
            private readonly Action<T> action;

            public HandlerMethodWrapper(Action<T> action)
            {
                this.action = action;
            }

            public void Handle(T message)
            {
                action(message);
            }
        }
    }
}