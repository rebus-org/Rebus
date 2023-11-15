using System;
using Rebus.Messages;
using Rebus.Pipeline.Send;
using Rebus.Transport;

namespace Rebus.Pipeline;

/// <summary>
/// Concrete derivation of <see cref="StepContext"/> that is meant to be used to pass down the pipeline for processing outgoing messages
/// </summary>
public class OutgoingStepContext : StepContext
{
    /// <summary>
    /// Constructs the step context, initially stashing the given <see cref="Message"/>, list of <see cref="DestinationAddresses"/> and <see cref="ITransactionContext"/> into its bag of objects
    /// </summary>
    public OutgoingStepContext(Message logicalMessage, ITransactionContext transactionContext, DestinationAddresses destinationAddresses)
    {
        if (logicalMessage == null) throw new ArgumentNullException(nameof(logicalMessage));
        if (transactionContext == null) throw new ArgumentNullException(nameof(transactionContext));
        if (destinationAddresses == null) throw new ArgumentNullException(nameof(destinationAddresses));

        Save(logicalMessage);
        Save(destinationAddresses);
        Save(transactionContext);
    }
}