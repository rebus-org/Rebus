using System;
using System.Collections.Generic;

namespace Rebus.Testing
{
    /// <summary>
    /// Implementation of <see cref="IBus"/> that is meant to be used in tests. Collects
    /// all sent messages and headers etc., allowing tests to inspect them during the
    /// assert phase of the test.
    /// </summary>
    public class FakeBus : IBus
    {
        readonly List<object> sentMessages = new List<object>();
        readonly List<object> publishedMessages = new List<object>();
        readonly List<object> locallySentMessages = new List<object>();
        readonly List<object> replies = new List<object>();
        readonly List<Type> subscriptions = new List<Type>();
        readonly List<Type> unsubscriptions = new List<Type>();
        readonly List<DeferredMessage> deferredMessages = new List<DeferredMessage>();

        readonly Dictionary<object, Dictionary<string, string>> attachedHeaders = new Dictionary<object, Dictionary<string, string>>();

        /// <summary>
        /// Stores the message in the list of implicitly routed sent messages: <see cref="SentMessages"/>
        /// </summary>
        public void Send<TCommand>(TCommand message)
        {
            sentMessages.Add(message);
        }

        /// <summary>
        /// Stores the message in the list of message explicitly sent to self: <see cref="LocallySentMessages"/>
        /// </summary>
        public void SendLocal<TCommand>(TCommand message)
        {
            locallySentMessages.Add(message);
        }

        /// <summary>
        /// Stores the message in the list of sent replies: <see cref="Replies"/>
        /// </summary>
        public void Reply<TResponse>(TResponse message)
        {
            replies.Add(message);
        }

        /// <summary>
        /// Stores the event type in the list of implicitly routed subscribed types: <see cref="Subscriptions"/>
        /// </summary>
        public void Subscribe<TEvent>()
        {
            subscriptions.Add(typeof(TEvent));
        }

        /// <summary>
        /// Stores the event type in the list of implicitly routed unsubscribed types: <see cref="Unsubscriptions"/>
        /// </summary>
        public void Unsubscribe<TEvent>()
        {
            unsubscriptions.Add(typeof (TEvent));
        }

        /// <summary>
        /// Stores the message in the list of published events: <see cref="PublishedMessages"/>
        /// </summary>
        public void Publish<TEvent>(TEvent message)
        {
            publishedMessages.Add(message);
        }

        /// <summary>
        /// Stores the message in the list of deferred messages: <see cref="DeferredMessages"/>
        /// </summary>
        public void Defer(TimeSpan delay, object message)
        {
            deferredMessages.Add(new DeferredMessage(message, delay));
        }

        /// <summary>
        /// Stores information about the attached header in the list of header attachments made: <see cref="AttachedHeaders"/>
        /// </summary>
        public void AttachHeader(object message, string key, string value)
        {
            if (!attachedHeaders.ContainsKey(message))
            {
                attachedHeaders[message] = new Dictionary<string, string>();
            }

            attachedHeaders[message][key] = value;
        }

        /// <summary>
        /// Would have accessed the advanced API, but the FakeBus does not support advanced operations (yet... it might, sometime in the future)
        /// </summary>
        public IAdvancedBus Advanced { get { throw new NotSupportedException("sorry, but FakeBus does not support advanced operations (yet)"); } }

        /// <summary>
        /// Accesses the accumulated list of sent messages.
        /// </summary>
        public List<object> SentMessages
        {
            get { return sentMessages; }
        }

        /// <summary>
        /// Accesses the accumulated list of published messages.
        /// </summary>
        public List<object> PublishedMessages
        {
            get { return publishedMessages; }
        }

        /// <summary>
        /// Accesses the accumulated list messages sent to self.
        /// </summary>
        public List<object> LocallySentMessages
        {
            get { return locallySentMessages; }
        }

        /// <summary>
        /// Accesses the accumulated list of replies sent.
        /// </summary>
        public List<object> Replies
        {
            get { return replies; }
        }

        /// <summary>
        /// Accesses the accumulated list of subscriptions made.
        /// </summary>
        public List<Type> Subscriptions
        {
            get { return subscriptions; }
        }

        /// <summary>
        /// Accesses the accumulated list of unsubscriptions made.
        /// </summary>
        public List<Type> Unsubscriptions
        {
            get { return unsubscriptions; }
        }

        /// <summary>
        /// Accesses the accumulated list of messages deferred to the future.
        /// </summary>
        public List<DeferredMessage> DeferredMessages
        {
            get { return deferredMessages; }
        }

        /// <summary>
        /// Accesses the accumulated list of attached headers.
        /// </summary>
        public Dictionary<object, Dictionary<string, string>> AttachedHeaders
        {
            get { return attachedHeaders; }
        }

        /// <summary>
        /// Accesses the accumulated list of attached headers for one specific message.
        /// </summary>
        public Dictionary<string, string> GetAttachedHeaders(object message)
        {
            return attachedHeaders.ContainsKey(message)
                       ? attachedHeaders[message]
                       : new Dictionary<string, string>();
        }

        /// <summary>
        /// Doesn't do anything
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Contains information about a message deferred to the future.
        /// </summary>
        public class DeferredMessage
        {
            internal DeferredMessage(object message, TimeSpan delay)
            {
                Message = message;
                Delay = delay;
            }

            /// <summary>
            /// Gets the message that was deferred
            /// </summary>
            public object Message { get; private set; }
            
            /// <summary>
            /// Gets the delay by which this message was deferred
            /// </summary>
            public TimeSpan Delay { get; private set; }
        }
    }
}