using System;
using System.Collections.Generic;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Pipeline;

/// <summary>
/// Implementation of <see cref="IMessageContext"/> that provides the static gateway <see cref="Current"/> property to get
/// the current message context.
/// </summary>
public class MessageContext : IMessageContext
{
    internal MessageContext(ITransactionContext transactionContext)
    {
        TransactionContext = transactionContext ?? throw new ArgumentNullException(nameof(transactionContext));
    }

    /// <summary>
    /// This is the outermost context, the one that spans the entire queue receive transaction. The other properties on the message
    /// context are merely provided as a convenience.
    /// </summary>
    public ITransactionContext TransactionContext { get; }

    /// <summary>
    /// Gets the step context, i.e. the context that is passed down through the step pipeline when a message is received.
    /// </summary>
    public IncomingStepContext IncomingStepContext => TransactionContext.GetOrThrow<IncomingStepContext>(StepContext.StepContextKey);

    /// <summary>
    /// Gets the <see cref="IMessageContext.TransportMessage"/> model for the message currently being handled.
    /// </summary>
    public TransportMessage TransportMessage => IncomingStepContext.Load<TransportMessage>();

    /// <summary>
    /// Gets the <see cref="IMessageContext.Message"/> model for the message currently being handled.
    /// </summary>
    public Message Message => IncomingStepContext.Load<Message>();

    /// <summary>
    /// Gets the headers dictionary of the incoming message (same as accessing the Headers of the context's transport message,
    /// or the logical message if the message has been properly deserialized)
    /// </summary>
    public Dictionary<string, string> Headers => TransportMessage.Headers;

    /// <summary>
    /// Gets the current message context from the current <see cref="AmbientTransactionContext"/> (accessed by
    /// <see cref="AmbientTransactionContext.Current"/>), returning null if no transaction context was found
    /// </summary>
    public static IMessageContext Current
    {
        get
        {
            var transactionContext = AmbientTransactionContext.Current;

            return transactionContext != null && transactionContext.Items.ContainsKey(StepContext.StepContextKey)
                ? new MessageContext(transactionContext)
                : null;
        }
    }
}