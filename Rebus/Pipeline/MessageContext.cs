using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Pipeline
{
    /// <summary>
    /// Implementation of <see cref="IMessageContext"/> that provides the static gateway <see cref="Current"/> property to get
    /// the current message context.
    /// </summary>
    public class MessageContext : IMessageContext
    {
        /// <summary>
        /// This is the outermost context, the one that spans the entire queue receive transaction. The other properties on the message
        /// context are merely provided as a convenience.
        /// </summary>
        public ITransactionContext TransactionContext { get; private set; }

        /// <summary>
        /// Gets the step context, i.e. the context that is passed down through the step pipeline when a message is received.
        /// </summary>
        public IncomingStepContext IncomingStepContext
        {
            get { return TransactionContext.GetOrThrow<IncomingStepContext>(StepContext.StepContextKey); }
        }

        /// <summary>
        /// Gets the <see cref="IMessageContext.TransportMessage"/> model for the message currently being handled.
        /// </summary>
        public TransportMessage TransportMessage
        {
            get { return IncomingStepContext.Load<TransportMessage>(); }
        }

        /// <summary>
        /// Gets the <see cref="IMessageContext.Message"/> model for the message currently being handled.
        /// </summary>
        public Message Message
        {
            get { return IncomingStepContext.Load<Message>(); }
        }

        MessageContext(ITransactionContext transactionContext)
        {
            TransactionContext = transactionContext;
        }

        /// <summary>
        /// Gets the current message context from the current <see cref="AmbientTransactionContext"/> (accessed by
        /// <see cref="AmbientTransactionContext.Current"/>), returning null if no transaction context was found
        /// </summary>
        public static IMessageContext Current
        {
            get
            {
                var transactionContext = AmbientTransactionContext.Current;
                return transactionContext == null ? null : new MessageContext(transactionContext);
            }
        }
    }
}