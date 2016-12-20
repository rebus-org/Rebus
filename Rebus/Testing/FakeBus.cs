using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.Messages;
using Rebus.Routing;
using Rebus.Testing.Events;

#pragma warning disable 1998

namespace Rebus.Testing
{
    /// <summary>
    /// The fake bus is an implementation of <see cref="IBus"/> that can be used for testing. The fake bus
    /// just collects information about what has happened to it, allowing you to query that information after the
    /// fact by checking <see cref="Events"/>
    /// </summary>
    public class FakeBus : IBus
    {
        readonly ConcurrentQueue<FakeBusEvent> _events = new ConcurrentQueue<FakeBusEvent>();
        readonly List<Delegate> _callbacks = new List<Delegate>();

        IAdvancedApi _advancedApi;

        /// <summary>
        /// Gets all events recorded at this point. Query this in order to check what happened to the fake bus while
        /// it participated in a test - e.g. like this:
        /// <code>
        /// await fakeBus.Send(new MyMessage("woohoo!"));
        ///
        /// var sentMessagesWithMyGreeting = fakeBus.Events
        ///     .OfType&lt;MessageSent&lt;MyMessage&gt;&gt;()
        ///     .Count(m => m.CommandMessage.Text == "woohoo!");
        ///
        /// Assert.That(sentMessagesWithMyGreeting, Is.EqualTo(1));
        /// </code>
        /// </summary>
        public IEnumerable<FakeBusEvent> Events => _events.ToList();

        /// <summary>
        /// Adds a callback to be invoked when new events are recorded in the fake bus
        /// </summary>
        public void On<TEvent>(Action<TEvent> callback) where TEvent : FakeBusEvent
        {
            _callbacks.Add(callback);
        }

        /// <summary>
        /// Clears all events recorded by the fake bus. Registered callbacks will NOT be cleared
        /// </summary>
        public void Clear()
        {
            FakeBusEvent instance;
            while (_events.TryDequeue(out instance)) { }
        }

        /// <summary>
        /// Sends the specified message to our own input queue address
        /// </summary>
        public async Task SendLocal(object commandMessage, Dictionary<string, string> optionalHeaders = null)
        {
            var messageSentToSelfEvent = CreateEventGeneric<MessageSentToSelf>(typeof(MessageSentToSelf<>), commandMessage.GetType(), commandMessage, optionalHeaders);

            Record(messageSentToSelfEvent);
        }

        /// <summary>
        /// Sends the specified message to a destination that is determined by calling <see cref="IRouter.GetDestinationAddress"/>
        /// </summary>
        public async Task Send(object commandMessage, Dictionary<string, string> optionalHeaders = null)
        {
            var messageSentEvent = CreateEventGeneric<MessageSent>(typeof(MessageSent<>), commandMessage.GetType(), commandMessage, optionalHeaders);

            Record(messageSentEvent);
        }

        /// <summary>
        /// Sends the specified reply message to a destination that is determined by looking up the <see cref="Headers.ReturnAddress"/> header of the message currently being handled.
        /// This method can only be called from within a message handler.
        /// </summary>
        public async Task Reply(object replyMessage, Dictionary<string, string> optionalHeaders = null)
        {
            var replyMessageSentEvent = CreateEventGeneric<ReplyMessageSent>(typeof(ReplyMessageSent<>), replyMessage.GetType(), replyMessage, optionalHeaders);

            Record(replyMessageSentEvent);
        }

        /// <summary>
        /// Defers the delivery of the message by attaching a <see cref="Headers.DeferredUntil"/> header to it and delivering it to the configured timeout manager endpoint
        /// (defaults to be ourselves). When the time is right, the deferred message is returned to the address indicated by the <see cref="Headers.ReturnAddress"/> header.
        /// </summary>
        public async Task Defer(TimeSpan delay, object message, Dictionary<string, string> optionalHeaders = null)
        {
            var messageDeferredEvent = CreateEventGeneric<MessageDeferred>(typeof(MessageDeferred<>), message.GetType(), delay, message, optionalHeaders);

            Record(messageDeferredEvent);
        }

        /// <summary>
        /// Gets the advanced API (which is not currently supported for the fake bus - throws an <see cref="InvalidOperationException"/> at the moment)
        /// </summary>
        public IAdvancedApi Advanced
        {
            get
            {
                if (_advancedApi != null) return _advancedApi;

                throw new InvalidOperationException("This FakeBus instance does not have an advanced API - you can add one by setting the Advanced property, e.g. to an instance of FakeAdvancedApi that you can then customize to your needs");
            }
            set { _advancedApi = value; }
        }

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
        public async Task Subscribe<TEvent>()
        {
            Record(new Subscribed(typeof(TEvent)));
        }

        /// <summary>
        /// Subscribes to the topic defined by the assembly-qualified name of <paramref name="eventType"/>. 
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
        public async Task Subscribe(Type eventType)
        {
            Record(new Subscribed(eventType));
        }

        /// <summary>
        /// Unsubscribes from the topic defined by the assembly-qualified name of <typeparamref name="TEvent"/>
        /// </summary>
        public async Task Unsubscribe<TEvent>()
        {
            Record(new Unsubscribed(typeof(TEvent)));
        }

        /// <summary>
        /// Unsubscribes from the topic defined by the assembly-qualified name of <paramref name="eventType"/>
        /// </summary>
        public async Task Unsubscribe(Type eventType)
        {
            Record(new Unsubscribed(eventType));
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
        public async Task Publish(object eventMessage, Dictionary<string, string> optionalHeaders = null)
        {
            var messagePublishedEvent = CreateEventGeneric<MessagePublished>(typeof(MessagePublished<>), eventMessage.GetType(), eventMessage, optionalHeaders);

            Record(messagePublishedEvent);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Record(new FakeBusDisposed());
        }

        static TEvent CreateEventGeneric<TEvent>(Type openGeneric, Type closingType, params object[] args) where TEvent : FakeBusEvent
        {
            var eventType = CloseEventType(openGeneric, closingType);
            var constructor = GetConstructor(eventType);
            var instance = CreateInstance(constructor, args);
            try
            {
                return (TEvent)instance;
            }
            catch (Exception exception)
            {
                throw new InvalidCastException($"Could not turn created instance {instance} into a {typeof(TEvent)}", exception);
            }
        }

        static object CreateInstance(ConstructorInfo constructor, object[] args)
        {
            try
            {
                return constructor.Invoke(args);
            }
            catch (Exception exception)
            {
                throw new ArgumentException($"Invocation of constructor with signature ({string.Join(", ", constructor.GetParameters().Select(p => p.ParameterType))}) failed with args ({string.Join(", ", args)})", exception);
            }
        }

        static ConstructorInfo GetConstructor(Type eventType)
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance;

            var constructor = eventType.GetTypeInfo().GetConstructors(flags).FirstOrDefault();
            if (constructor != null)
            {
                return constructor;
            }

            throw new InvalidOperationException($"Could not find (non-public, instance-, create-instance-) constructor on {eventType}");
        }

        static Type CloseEventType(Type openGeneric, Type closingType)
        {
            try
            {
                return openGeneric.MakeGenericType(closingType);
            }
            catch (Exception exception)
            {
                throw new ArgumentException($"Could not close {openGeneric} with {closingType}", exception);
            }
        }

        void Record(FakeBusEvent fakeBusEvent)
        {
            AddFakeBusEvent(fakeBusEvent);

            InvokeCompatibleCallbacks(fakeBusEvent);
        }

        void AddFakeBusEvent(FakeBusEvent fakeBusEvent)
        {
            _events.Enqueue(fakeBusEvent);
        }

        void InvokeCompatibleCallbacks(FakeBusEvent fakeBusEvent)
        {
            foreach (var callback in _callbacks)
            {
                var compatibleHandlerType = typeof (Action<>).MakeGenericType(fakeBusEvent.GetType());

                if (!compatibleHandlerType.GetTypeInfo().IsInstanceOfType(callback)) continue;

                try
                {
                    callback.DynamicInvoke(fakeBusEvent);
                }
                catch (Exception exception)
                {
                    throw new TargetInvocationException($"Error invoking callback for fake bus event {fakeBusEvent}", exception);
                }
            }
        }
    }
}