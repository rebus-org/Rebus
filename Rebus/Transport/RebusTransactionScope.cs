using System;
using System.Threading.Tasks;
using Rebus.Bus.Advanced;

namespace Rebus.Transport;

/// <summary>
/// Default (async, as in: the <see cref="CompleteAsync"/> method returns a <see cref="Task"/> to be awaited) transaction scope 
/// that sets up an ambient <see cref="ITransactionContext"/> and removes
/// it when the scope is disposed. Call <code>await scope.Complete();</code> in order to end the scope
/// by committing any actions enlisted to be executed.
/// </summary>
public class RebusTransactionScope : IDisposable
{
    readonly ITransactionContext _previousTransactionContext = AmbientTransactionContext.Current;
    readonly TransactionContext _transactionContext = new();

    /// <summary>
    /// Creates a new transaction context and mounts it on <see cref="AmbientTransactionContext.Current"/>, making it available for Rebus
    /// to pick up. The context can also be retrieved simply via <see cref="TransactionContext"/>
    /// </summary>
    public RebusTransactionScope() => AmbientTransactionContext.SetCurrent(_transactionContext);

    /// <summary>
    /// Gets the transaction context instance that this scope is holding
    /// </summary>
    public ITransactionContext TransactionContext => _transactionContext;

    /// <summary>
    /// Ends the current transaction by either committing it or aborting it, depending on whether someone voted for abortion
    /// </summary>
    public Task CompleteAsync()
    {
        _transactionContext.SetResult(commit: true, ack: true);
        return _transactionContext.Complete();
    }

    /// <summary>
    /// Ends the current transaction by either committing it or aborting it, depending on whether someone voted for abortion (synchronous version)
    /// </summary>
    public void Complete() => AsyncHelpers.RunSync(CompleteAsync);

    /// <summary>
    /// Disposes the transaction context and removes it from <see cref="AmbientTransactionContext.Current"/> again
    /// </summary>
    public void Dispose()
    {
        try
        {
            _transactionContext.Dispose();
        }
        finally
        {
            AmbientTransactionContext.SetCurrent(_previousTransactionContext);
        }
    }
}