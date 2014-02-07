using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rebus
{
    /// <summary>
    /// Very simple implementation of the handler activator that allows a bunch of types to be manually registered,
    /// either with their type or with a factoryMethod method.
    /// </summary>
    public class SimpleHandlerActivator : IActivateHandlers
    {
        readonly Dictionary<Type, List<Func<object>>> activators = new Dictionary<Type, List<Func<object>>>();

        /// <summary>
        /// Registers the given handler type. It is assumed that the type registered has a public
        /// default constructor - otherwise, instantiation will fail.
        /// </summary>
        public SimpleHandlerActivator Register(Type handlerType)
        {
            InnerRegister(handlerType, () => Activator.CreateInstance(handlerType));

            return this;
        }

        /// <summary>
        /// Registers a factoryMethod method that is capable of creating a handler instance.
        /// </summary>
        public SimpleHandlerActivator Register<THandler>(Func<THandler> handlerFactory)
        {
            InnerRegister(typeof(THandler), () => handlerFactory());

            return this;
        }

        /// <summary>
        /// Registers a function that can handle messages of the specified type.
        /// </summary>
        public SimpleHandlerActivator Handle<TMessage>(Action<TMessage> handler)
        {
            InnerRegister(typeof(HandlerMethodWrapper<TMessage>), () => new HandlerMethodWrapper<TMessage>(handler));
            
            return this;
        }

        void InnerRegister(Type handlerType, Func<object> factoryMethod)
        {
            var handlerInterfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IHandleMessages<>));

            foreach (var handlerInterface in handlerInterfaces)
            {
                if (!activators.ContainsKey(handlerInterface))
                    activators[handlerInterface] = new List<Func<object>>();

                activators[handlerInterface].Add(factoryMethod);
            }
        }

        /// <summary>
        /// Gets all available handlers that can be cast to <see cref="IHandleMessages{TMessage}"/>
        /// </summary>
        public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
        {
            if (!activators.ContainsKey(typeof(IHandleMessages<T>)))
                return new IHandleMessages<T>[0];

            return activators[typeof(IHandleMessages<T>)]
                .Select(f => f()).Cast<IHandleMessages<T>>()
                .ToArray();
        }

        /// <summary>
        /// Loops throug the given sequence of handler instances and disposes those that implement <see cref="IDisposable"/>.
        /// Obviously, this way of disposing dispoables is not as powerful as e.g. Windsor's way of doing it, do you'll
        /// have to be sure that handlers that YOU know are singletons, are not disposable.
        /// </summary>
        public void Release(IEnumerable handlerInstances)
        {
            foreach (var instance in handlerInstances)
            {
                if (!(instance is IDisposable)) continue;

                ((IDisposable) instance).Dispose();
            }
        }

        class HandlerMethodWrapper<T> : IHandleMessages<T>
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