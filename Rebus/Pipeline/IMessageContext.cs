using System.Collections.Generic;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Pipeline;

/// <summary>
/// Representation of the current "message context", which is a convenience wrapper that gives access to the current <see cref="ITransactionContext"/>
/// (which is the outermost context, the one that spans the entire queue receive transaction) and the current
/// <see cref="IncomingStepContext"/> (which is actually contained in the transaction context).
/// </summary>
public interface IMessageContext
{
    /// <summary>
    /// This is the outermost context, the one that spans the entire queue receive transaction. The other properties on the message
    /// context are merely provided as a convenience.
    /// </summary>
    ITransactionContext TransactionContext { get; }

    /// <summary>
    /// Gets the step context, i.e. the context that is passed down through the step pipeline when a message is received.
    /// </summary>
    IncomingStepContext IncomingStepContext { get; }

    /// <summary>
    /// Gets the <see cref="TransportMessage"/> model for the message currently being handled.
    /// </summary>
    TransportMessage TransportMessage { get; }

    /// <summary>
    /// Gets the <see cref="Message"/> model for the message currently being handled.
    /// </summary>
    Message Message { get; }

    /// <summary>
    /// Gets the headers dictionary of the incoming message (same as accessing the Headers of the context's transport message,
    /// or the logical message if the message has been properly deserialized)
    /// </summary>
    Dictionary<string,string> Headers { get; }
}