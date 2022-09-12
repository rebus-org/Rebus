using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Pipeline;

/// <summary>
/// Concrete derivation of <see cref="StepContext"/> that is meant to be used to pass down the pipeline for processing incoming messages
/// </summary>
public class IncomingStepContext : StepContext, IDisposable
{
    readonly IPrincipal _originalPrincipal = Thread.CurrentPrincipal;

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

    /// <summary>
    /// Gets/sets the identity under which the incoming message should be handled. Setting this will immediately change
    /// <see cref="Thread.CurrentPrincipal"/> to the given principal.
    /// </summary>
    public ClaimsPrincipal User
    {
        get => Load<ClaimsPrincipal>();
        set
        {
            Save(value);
            Load<ITransactionContext>().Items["custom-claims-context"] = value;
            Thread.CurrentPrincipal = value;
        }
    }

    /// <summary>
    /// Restores <see cref="Thread.CurrentPrincipal"/> to the value it had when <see cref="IncomingStepContext"/> was constructed
    /// </summary>
    public void Dispose() => Thread.CurrentPrincipal = _originalPrincipal;
}