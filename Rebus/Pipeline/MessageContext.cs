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
        public ITransactionContext TransactionContext { get; private set; }

        public IncomingStepContext IncomingStepContext
        {
            get { return TransactionContext.Items.GetOrThrow<IncomingStepContext>(StepContext.StepContextKey); }
        }

        public TransportMessage TransportMessage
        {
            get { return IncomingStepContext.Load<TransportMessage>(); }
        }

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
                if (transactionContext == null) return null;
                
                return new MessageContext(transactionContext);
            }
        }
    }
}