using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Pipeline;

/// <summary>
/// Concrete derivation of <see cref="StepContext"/> that is meant to be used to pass down the pipeline for processing incoming messages
/// </summary>
public class IncomingStepContext : StepContext
{
    /// <summary>
    /// Constructs the step context, initially stashing the given <see cref="TransportMessage"/> and <see cref="ITransactionContext"/> into its bag of objects
    /// </summary>
    public IncomingStepContext(TransportMessage message, ITransactionContext transactionContext)
    {
        Save(message);
        Save(new OriginalTransportMessage(message));
        Save(transactionContext);
            
        transactionContext.Items[StepContextKey] = this;
    }
}