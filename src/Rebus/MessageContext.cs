using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Extensions;

namespace Rebus
{
    /// <summary>
    /// Holds information about the message currently being handled on this particular thread.
    /// </summary>
    public class MessageContext : IMessageContext
    {
        const string DispatchMessageToHandlersKey = "rebus-DispatchMessageToHandlers";
        const string MessageContextItemKey = "rebus-message-context";
        readonly IDictionary<string, object> headers;
        static ILog log;

        static MessageContext()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        /// <summary>
        /// Contains the headers dictionary of the transport message currently being handled.
        /// </summary>
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

        /// <summary>
        /// Gets a reference to the current logical message being handled
        /// </summary>
        public object CurrentMessage
        {
            get { return currentMessage; }
        }

        internal static void Restablish(IMessageContext context)
        {
            current = context;
        }

        internal static MessageContext Establish(IDictionary<string, object> headers)
        {
            var messageContext = new MessageContext(headers);

            CallContext.LogicalSetData("context", messageContext);

            if (TransactionContext.Current != null)
            {
                if (TransactionContext.Current[MessageContextItemKey] != null)
                {
                    throw new InvalidOperationException(
                        string.Format("Cannot establish new message context when one is already present!"));
                }
                TransactionContext.Current[MessageContextItemKey] = messageContext;
            }
            else
            {
                if (current != null)
                {
                    throw new InvalidOperationException(
                        string.Format("Cannot establish new message context when one is already present"));
                }
                current = messageContext;
            }

            return messageContext;
        }

        MessageContext(IDictionary<string, object> headers)
        {
            this.headers = headers;

            Items = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets the message ID from the transport message headers
        /// </summary>
        public string RebusTransportMessageId
        {
            get { return (string)headers.ValueOrNull(Shared.Headers.MessageId); }
        }

        /// <summary>
        /// Gets the return address from the transport message headers. This address will most likely be the sender
        /// of message currently being handled, but it could also have been set explicitly by the sender to another
        /// endpoint
        /// </summary>
        public string ReturnAddress
        {
            get { return (string)headers.ValueOrNull(Shared.Headers.ReturnAddress); }
        }

        /// <summary>
        /// Gets the dictionary of objects associated with this message context. This collection can be used to store stuff
        /// for the duration of the handling of this transport message.
        /// </summary>
        public IDictionary<string, object> Items { get; private set; }

        /// <summary>
        /// Gets the current thread-bound message context if one is available, throwing an <see cref="InvalidOperationException"/>
        /// otherwise. Use <seealso cref="HasCurrent"/> to check if a message context is available if you're unsure
        /// </summary>
        public static IMessageContext GetCurrent()
        {
            if (TransactionContext.Current != null)
            {
                var context = TransactionContext.Current[MessageContextItemKey] as IMessageContext;
                if (context == null)
                {
                    throw new InvalidOperationException(string.Format("Could not find message context! Looked for it in the current transaction context: {0}", TransactionContext.Current));
                }
                return context;
            }

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
            get
            {
                if (TransactionContext.Current != null)
                {
                    var messageContext = TransactionContext.Current[MessageContextItemKey] as IMessageContext;
                    
                    return messageContext != null;
                }
                return current != null;
            }
        }

        /// <summary>
        /// Indicates whether message dispatch has been aborted in this message context
        /// </summary>
        public static bool MessageDispatchAborted
        {
            get
            {
                if (!HasCurrent) return false;

                var messageContext = GetCurrent();

                return messageContext.Items.ContainsKey(DispatchMessageToHandlersKey)
                       && !(bool)messageContext.Items[DispatchMessageToHandlersKey];
            }
        }

        /// <summary>
        /// Aborts processing the current message - i.e., after exiting from the
        /// current handler, no more handlers will be called. Note that this does
        /// not cause the current transaction to be rolled back.
        /// </summary>
        public void Abort()
        {
            log.Debug("Abort was called - will stop dispatching message to handlers");
            Items[DispatchMessageToHandlersKey] = false;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (TransactionContext.Current != null)
            {
                TransactionContext.Current[MessageContextItemKey] = null;
            }
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