using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rebus.Configuration
{
    /// <summary>
    /// Very simple and independent container adapter that relies on <see cref="SimpleHandlerActivator"/>
    /// to activate handlers.
    /// </summary>
    public class BuiltinContainerAdapter : IContainerAdapter, IDisposable
    {
        readonly SimpleHandlerActivator handlerActivator = new SimpleHandlerActivator();

        /// <summary>
        /// Use this property to access the bus instance
        /// </summary>
        public IBus Bus { get; internal set; }

        /// <summary>
        /// Registers the given handler type. It is assumed that the type registered has a public
        /// default constructor - otherwise, instantiation will fail.
        /// </summary>
        public BuiltinContainerAdapter Register(Type handlerType)
        {
            handlerActivator.Register(handlerType);
            return this;
        }

        /// <summary>
        /// Registers a factory method that is capable of creating a handler instance.
        /// </summary>
        public BuiltinContainerAdapter Register<THandler>(Func<THandler> handlerFactory)
        {
            handlerActivator.Register(handlerFactory);
            return this;
        }

        /// <summary>
        /// Registers a function that can handle messages of the specified type.
        /// </summary>
        public BuiltinContainerAdapter Handle<TMessage>(Action<TMessage> handler)
        {
            handlerActivator.Handle(handler);
            return this;
        }

        /// <summary>
        /// Registers a function that can handle messages of the specified type.
        /// </summary>
        public BuiltinContainerAdapter HandleAsync<TMessage>(Func<TMessage, Task> handler)
        {
            handlerActivator.HandleAsync(handler);
            return this;
        }

        /// <summary>
        /// Uses the underlying <see cref="SimpleHandlerActivator"/> to look up handler instances
        /// that can handle messages of type <typeparamref name="T"/>
        /// </summary>
        public IEnumerable<IHandleMessages> GetHandlerInstancesFor<T>()
        {
            return handlerActivator.GetHandlerInstancesFor<T>();
        }

        /// <summary>
        /// Uses the underlying <see cref="SimpleHandlerActivator"/> to release the given handler instances
        /// </summary>
        public void Release(IEnumerable handlerInstances)
        {
            handlerActivator.Release(handlerInstances);
        }

        /// <summary>
        /// Saves the given <see cref="IBus"/> reference for later use
        /// </summary>
        public void SaveBusInstances(IBus bus)
        {
            if (!ReferenceEquals(null, Bus))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "You can't call SaveBusInstances twice on the container adapter! Already have bus instance {0} when you tried to overwrite it with {1}",
                        Bus, bus));
            }

            Bus = bus;
        }

        /// <summary>
        /// Makes sure that the referenced <see cref="IBus"/> is disposed
        /// </summary>
        public void Dispose()
        {
            if (ReferenceEquals(null, Bus)) return;
            Bus.Dispose();
        }
    }
}