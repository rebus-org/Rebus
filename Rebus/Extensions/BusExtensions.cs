using System;
using System.Threading.Tasks;
using Rebus.Bus;

namespace Rebus.Extensions
{
    /// <summary>
    /// Provides extensions to <see cref="IBus"/>
    /// </summary>
    public static class BusExtensions
    {
        /// <summary>
        /// Subscribes to the topic defined by the assembly-qualified name of <typeparamref name="TEvent"/>
        /// </summary>
        public static Task Subscribe<TEvent>(this IBus bus)
        {
            if (bus == null) throw new ArgumentNullException("bus");
            
            var topic = typeof(TEvent).AssemblyQualifiedName;
            
            return bus.Subscribe(topic);
        }

        /// <summary>
        /// Unsubscribes from the topic defined by the assembly-qualified name of <typeparamref name="TEvent"/>
        /// </summary>
        public static Task Unsubscribe<TEvent>(this IBus bus)
        {
            if (bus == null) throw new ArgumentNullException("bus");

            var topic = typeof(TEvent).AssemblyQualifiedName;

            return bus.Unsubscribe(topic);
        }
    }
}