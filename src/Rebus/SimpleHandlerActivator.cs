using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rebus
{
    /// <summary>
    /// Very simple implementation of the handler activator that allows a bunch of types to be manually registered,
    /// either with their type or with a factory method.
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
        /// Registers a factory method that is capable of creating a handler instance.
        /// </summary>
        public SimpleHandlerActivator Register<THandler>(Func<THandler> handlerFactory)
        {
            InnerRegister(typeof(THandler), () => handlerFactory());

            return this;
        }

        void InnerRegister(Type handlerType, Func<object> factory)
        {
            var handlerInterfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IHandleMessages<>));

            foreach (var handlerInterface in handlerInterfaces)
            {
                if (!activators.ContainsKey(handlerInterface))
                    activators[handlerInterface] = new List<Func<object>>();

                activators[handlerInterface].Add(factory);
            }
        }

        public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
        {
            if (!activators.ContainsKey(typeof(IHandleMessages<T>)))
                return new IHandleMessages<T>[0];

            return activators[typeof(IHandleMessages<T>)]
                .Select(f => f()).Cast<IHandleMessages<T>>()
                .ToArray();
        }

        public void Release(IEnumerable handlerInstances)
        {
        }
    }
}