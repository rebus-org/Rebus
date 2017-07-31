using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Bus.Advanced;
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
        readonly FakeBusEventRecorder _recorder = new FakeBusEventRecorder();
        readonly FakeBusEventFactory _factory = new FakeBusEventFactory();

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
        public IEnumerable<FakeBusEvent> Events => _recorder.GetEvents();

        /// <summary>
        /// Adds a callback to be invoked when new events are recorded in the fake bus
        /// </summary>
        public void On<TEvent>(Action<TEvent> callback) where TEvent : FakeBusEvent
        {
            _recorder.AddCallback(callback);
        }

        /// <summary>
        /// Clears all events recorded by the fake bus. Registered callbacks will NOT be cleared
        /// </summary>
        public void Clear()
        {
            _recorder.Clear();
        }

        /// <inheritdoc />
        public async Task SendLocal(object commandMessage, Dictionary<string, string> optionalHeaders = null)
        {
            var messageSentToSelfEvent = _factory.CreateEventGeneric<MessageSentToSelf>(typeof(MessageSentToSelf<>), commandMessage.GetType(), commandMessage, optionalHeaders);

            Record(messageSentToSelfEvent);
        }

        /// <inheritdoc />
        public async Task Send(object commandMessage, Dictionary<string, string> optionalHeaders = null)
        {
            var messageSentEvent = _factory.CreateEventGeneric<MessageSent>(typeof(MessageSent<>), commandMessage.GetType(), commandMessage, optionalHeaders);

            Record(messageSentEvent);
        }

        /// <inheritdoc />
        public async Task Reply(object replyMessage, Dictionary<string, string> optionalHeaders = null)
        {
            var replyMessageSentEvent = _factory.CreateEventGeneric<ReplyMessageSent>(typeof(ReplyMessageSent<>), replyMessage.GetType(), replyMessage, optionalHeaders);

            Record(replyMessageSentEvent);
        }

        /// <inheritdoc />
        public async Task Defer(TimeSpan delay, object message, Dictionary<string, string> optionalHeaders = null)
        {
            var messageDeferredEvent = _factory.CreateEventGeneric<MessageDeferred>(typeof(MessageDeferred<>), message.GetType(), delay, message, optionalHeaders);

            Record(messageDeferredEvent);
        }

        /// <inheritdoc />
        public async Task DeferLocal(TimeSpan delay, object message, Dictionary<string, string> optionalHeaders = null)
        {
            var messageDeferredEvent = _factory.CreateEventGeneric<MessageDeferredToSelf>(typeof(MessageDeferredToSelf<>), message.GetType(), delay, message, optionalHeaders);

            Record(messageDeferredEvent);
        }

        /// <summary>
        /// Gets the advanced API. An implementation of <see cref="IAdvancedApi"/> must either be passed to the contructor, or one must be set using the
        /// <see cref="Advanced"/> property before calling the getter, otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// Check out <see cref="FakeAdvancedApi"/> for an easy way to pass your own implementations of e.g. <see cref="ISyncBus"/> etc.
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

        /// <inheritdoc />
        public async Task Subscribe<TEvent>()
        {
            Record(new Subscribed(typeof(TEvent)));
        }

        /// <inheritdoc />
        public async Task Subscribe(Type eventType)
        {
            Record(new Subscribed(eventType));
        }

        /// <inheritdoc />
        public async Task Unsubscribe<TEvent>()
        {
            Record(new Unsubscribed(typeof(TEvent)));
        }

        /// <inheritdoc />
        public async Task Unsubscribe(Type eventType)
        {
            Record(new Unsubscribed(eventType));
        }

        /// <inheritdoc />
        public async Task Publish(object eventMessage, Dictionary<string, string> optionalHeaders = null)
        {
            var messagePublishedEvent = _factory.CreateEventGeneric<MessagePublished>(typeof(MessagePublished<>), eventMessage.GetType(), eventMessage, optionalHeaders);

            Record(messagePublishedEvent);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Record(new FakeBusDisposed());
        }

        void Record(FakeBusEvent fakeBusEvent)
        {
            _recorder.Record(fakeBusEvent);
        }
    }
}