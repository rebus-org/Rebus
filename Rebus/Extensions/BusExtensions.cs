using System;
using System.Collections.Generic;
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
        /// Subscribes to the topic defined by the assembly-qualified name of <typeparamref name="TEvent"/>. 
        /// While this kind of subscription can work universally with the general topic-based routing, it works especially well with type-based routing,
        /// which can be enabled by going 
        /// <code>
        /// Configure.With(...)
        ///     .(...)
        ///     .Routing(r => r.TypeBased()
        ///             .Map&lt;SomeMessage&gt;("someEndpoint")
        ///             .(...))
        /// </code>
        /// in the configuration
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

        /// <summary>
        /// Publishes the event message on the topic defined by the assembly-qualified name of the type of the message.
        /// While this kind of pub/sub can work universally with the general topic-based routing, it works especially well with type-based routing,
        /// which can be enabled by going 
        /// <code>
        /// Configure.With(...)
        ///     .(...)
        ///     .Routing(r => r.TypeBased()
        ///             .Map&lt;SomeMessage&gt;("someEndpoint")
        ///             .(...))
        /// </code>
        /// in the configuration
        /// </summary>
        public static Task Publish(this IBus bus, object eventMessage, Dictionary<string, string> optionalHeaders = null)
        {
            if (bus == null) throw new ArgumentNullException("bus");
            if (eventMessage == null) throw new ArgumentNullException("eventMessage");

            var messageType = eventMessage.GetType();

            return bus.Publish(messageType.AssemblyQualifiedName, eventMessage, optionalHeaders);
        }
    }
}