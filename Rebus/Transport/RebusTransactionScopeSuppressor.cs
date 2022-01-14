using System;

namespace Rebus.Transport;

/// <summary>
/// Rebus transaction scope suppressor obviates the effect of an already active ambient Rebus transaction.
/// E.g. inside Rebus handlers, this scope can be used if one wants to send/publish outgoing messages immediately,
/// instead of enlisting them in the current handler's transaction context.
/// </summary>
public class RebusTransactionScopeSuppressor : IDisposable
{
    readonly ITransactionContext _previousTransactionContext = AmbientTransactionContext.Current;

    /// <summary>
    /// Enters the scope by removing the currently entered ambient Rebus transaction context. The context
    /// will be restored, when the scope is disposed.
    /// </summary>
    public RebusTransactionScopeSuppressor() => AmbientTransactionContext.SetCurrent(null);

    /// <summary>
    /// Exits the scope, restoring the previously active ambient transaction context (if any)
    /// </summary>
    public void Dispose() => AmbientTransactionContext.SetCurrent(_previousTransactionContext);
}