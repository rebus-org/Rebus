using System;
using System.Collections.Generic;
using Rebus.Logging;
using Rebus.Extensions;

namespace Rebus
{
    /// <summary>
    /// Holds information about the message currently being handled on this particular thread.
    /// </summary>
    public class MessageContext : IMessageContext
    {
        readonly IDictionary<string, object> headers;
        static ILog log;

        static MessageContext()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        public IDictionary<string, object> Headers
        {
            get { return headers; }
        }

        /// <summary>
        /// Event that is raised when this message context instance is disposed
        /// </summary>
        public event Action Disposed = delegate { };

        [ThreadStatic]
        static internal IMessageContext current;
        object currentMessage;

        public object CurrentMessage
        {
            get { return currentMessage; }
        }

#if DEBUG
        /// <summary>
        /// Only applicable in DEBUG build: Stores that call stack of when this message context was created. Can be used to
        /// chase down bugs that happen when establishing a message context on a thread where a message context has already
        /// been established
        /// </summary>
        public string StackTrace { get; set; }
#endif

        internal static MessageContext Establish(IDictionary<string, object> headers)
        {
            if (current != null)
            {
#if DEBUG
                throw new InvalidOperationException(
                    string.Format(
                        @"Cannot establish new message context when one is already present!

Stacktrace of when the current message context was created:
{0}",
                        current.StackTrace));
#else
                throw new InvalidOperationException(
                    string.Format("Cannot establish new message context when one is already present"));
#endif

            }
            var messageContext = new MessageContext(headers);

            current = messageContext;

            return messageContext;
        }

        MessageContext(IDictionary<string, object> headers)
        {
            this.headers = headers;

            DispatchMessageToHandlers = true;
            Items = new Dictionary<string, object>();

#if DEBUG
            StackTrace = Environment.StackTrace;
#endif
        }

        public string TransportMessageId
        {
            get { return (string)headers.ValueOrNull(Shared.Headers.MessageId); }
        }
        
        public string ReturnAddress
        {
            get { return (string)headers.ValueOrNull(Shared.Headers.ReturnAddress); }
        }

        public IDictionary<string, object> Items { get; private set; }

        /// <summary>
        /// Gets the current thread-bound message context if one is available, throwing an <see cref="InvalidOperationException"/>
        /// otherwise. Use <seealso cref="HasCurrent"/> to check if a message context is available if you're unsure
        /// </summary>
        public static IMessageContext GetCurrent()
        {
            if (current == null)
            {
                throw new InvalidOperationException("No message context available - the MessageContext instance will"
                                                    + " only be set during the handling of messages, and it"
                                                    + " is available only on the worker thread.");
            }

            return current;
        }

        /// <summary>
        /// Indicates whether a message context is bound to the current thread
        /// </summary>
        public static bool HasCurrent
        {
            get { return current != null; }
        }

        /// <summary>
        /// Indicates whether message dispatch has been aborted in this message context
        /// </summary>
        public static bool MessageDispatchAborted
        {
            get { return HasCurrent && !((MessageContext)current).DispatchMessageToHandlers; }
        }

        internal bool DispatchMessageToHandlers { get; set; }

        public void Abort()
        {
            log.Debug("Abort was called - will stop dispatching message to handlers");
            DispatchMessageToHandlers = false;
        }

        public void Dispose()
        {
            current = null;
            Disposed();
        }

        /// <summary>
        /// Sets a reference to the logical message that is currently being handled
        /// </summary>
        public void SetLogicalMessage(object message)
        {
            currentMessage = message;
        }

        /// <summary>
        /// Clears the reference to the logical message that was being handled
        /// </summary>
        public void ClearLogicalMessage()
        {
            currentMessage = null;
        }
    }
}