using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
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
        readonly ISet<Type> handlersToSkip;
        static ILog log;

        static MessageContext()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        MessageContext(IDictionary<string, object> headers)
        {
            this.headers = headers;
            Items = new Dictionary<string, object>();
            handlersToSkip = new HashSet<Type>();
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

        /// <summary>
        /// Gets a reference to the current logical message being handled
        /// </summary>
        public object CurrentMessage { get; private set; }

        internal static MessageContext Establish()
        {
            return Establish(new Dictionary<string, object>());
        }

        internal static MessageContext Establish(IDictionary<string, object> headers)
        {
            var messageContext = new MessageContext(headers);
            Establish(messageContext, overwrite: false);
            return messageContext;
        }

        internal static void Establish(IMessageContext messageContext, bool overwrite)
        {
            if (TransactionContext.Current == null)
            {
                throw new InvalidOperationException(
                    string.Format("Could not find a transaction context. There should always be a transaction " +
                                  "context - though it might be a NoTransaction transaction context."));
            }

            if (TransactionContext.Current[MessageContextItemKey] != null && !overwrite)
            {
                throw new InvalidOperationException(
                    string.Format("Cannot establish new message context when one is already present!"));
            }

            TransactionContext.Current[MessageContextItemKey] = messageContext;
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
        /// Gets the handlers to skip.
        /// </summary>
        public IReadOnlyCollection<Type> HandlersToSkip { get { return new List<Type>(handlersToSkip).AsReadOnly(); } }

        /// <summary>
        /// Gets the current thread-bound message context if one is available, throwing an <see cref="InvalidOperationException"/>
        /// otherwise. Use <seealso cref="HasCurrent"/> to check if a message context is available if you're unsure
        /// </summary>
        public static IMessageContext GetCurrent()
        {
            if (TransactionContext.Current == null)
            {
                throw new InvalidOperationException(
                    string.Format("Could not find a transaction context. There should always be a transaction " +
                                  "context - though it might be a NoTransaction transaction context."));
            }

            var context = TransactionContext.Current[MessageContextItemKey] as IMessageContext;
            if (context == null)
            {
                throw new InvalidOperationException(
                    string.Format("Could not find message context! Looked for it in the current transaction context: {0}. " +
                                  "The MessageContext instance will only be set during the handling of messages.", TransactionContext.Current));
            }

            return context;
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

                return false;
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

            Disposed();
        }

        /// <summary>
        /// Sets a reference to the logical message that is currently being handled
        /// </summary>
        public void SetLogicalMessage(object message)
        {
            CurrentMessage = message;
        }

        /// <summary>
        /// Clears the reference to the logical message that was being handled
        /// </summary>
        public void ClearLogicalMessage()
        {
            CurrentMessage = null;
        }

        /// <summary>
        /// Instructs rebus handling infraestructure to skips the handler 
        /// specified by type on it's current invocation.
        /// </summary>
        /// <param name="type">The type.</param>
        public void SkipHandler(Type type)
        {
            if (!handlersToSkip.Contains(type))
            {
                handlersToSkip.Add(type);
            }
        }

        /// <summary>
        /// Removes the specified handler type from the list of handlers to skip.
        /// </summary>
        /// <param name="type">The type.</param>
        public void DoNotSkipHandler(Type type)
        {
            if (handlersToSkip.Contains(type))
            {
                handlersToSkip.Remove(type);
            }
        }
    }
}