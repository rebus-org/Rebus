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

        public event Action Disposed = delegate { };

        [ThreadStatic]
        static internal IMessageContext current;
        object currentMessage;

        public object CurrentMessage
        {
            get { return currentMessage; }
        }

#if DEBUG
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

        public static bool HasCurrent
        {
            get { return current != null; }
        }

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

        public void SetLogicalMessage(object message)
        {
            currentMessage = message;
        }

        public void ClearLogicalMessage()
        {
            currentMessage = null;
        }
    }
}