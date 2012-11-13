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

        public void Send<TCommand>(TCommand message)
        {
            sentMessages.Add(message);
        }

        public void SendLocal<TCommand>(TCommand message)
        {
            locallySentMessages.Add(message);
        }

        public void Reply<TResponse>(TResponse message)
        {
            replies.Add(message);
        }

        public void Subscribe<TEvent>()
        {
            subscriptions.Add(typeof(TEvent));
        }

        public void Unsubscribe<TEvent>()
        {
            unsubscriptions.Add(typeof (TEvent));
        }

        public void Publish<TEvent>(TEvent message)
        {
            publishedMessages.Add(message);
        }

        public void Defer(TimeSpan delay, object message)
        {
            deferredMessages.Add(new DeferredMessage(message, delay));
        }

        public void AttachHeader(object message, string key, string value)
        {
            if (attachedHeaders.ContainsKey(message))
                attachedHeaders[message] = new Dictionary<string, string>();

            attachedHeaders[message][key] = value;
        }

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

            public object Message { get; private set; }
            public TimeSpan Delay { get; private set; }
        }
    }
}