using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Rebus.Retry;

namespace Rebus.Transport;

/// <summary>
/// Represents the context of one queue transaction
/// </summary>
public interface ITransactionContext : IDisposable
{
    /// <summary>
    /// Stash of items that can carry stuff for later use in the transaction
    /// </summary>
    ConcurrentDictionary<string, object> Items { get; }

    /// <summary>
    /// Registers a listener to be called when the queue transaction is committed. This hook is reserved for the queue transaction
    /// and you may get unpredictable results of you enlist your own transaction in this
    /// </summary>
    void OnCommitted(Func<ITransactionContext, Task> commitAction);

    /// <summary>
    /// Registers a listener to be called when the queue transaction is aborted. This hook is reserved for the queue transaction
    /// and you may get unpredictable results of you enlist your own transaction in this
    /// </summary>
    void OnAborted(Action<ITransactionContext> abortedAction);

    /// <summary>
    /// Registers a listener to be called AFTER the queue transaction has been successfully committed (i.e. all listeners
    /// registered with <see cref="OnCommitted"/> have been executed). This would be a good place to complete the incoming
    /// message.
    /// </summary>
    void OnCompleted(Func<ITransactionContext, Task> completedAction);

    /// <summary>
    /// Registers a listener to be called after the transaction is over
    /// </summary>
    void OnDisposed(Action<ITransactionContext> disposedAction);

    /// <summary>
    /// Signals that something is wrong and the queue transaction must be aborted
    /// </summary>
    void Abort();

    /// <summary>
    /// Executes commit actions enlisted in the transaction with <see cref="OnCommitted"/>
    /// </summary>
    Task Commit();

    /// <summary>
    /// Marks the queue transaction as one that should not have outgoing messages committed, but still the incoming message will be ACKed.
    /// Probably only used in the single case where the <see cref="IErrorHandler"/> gets to handle the incoming message (e.g. by moving
    /// the message to the error queue)
    /// </summary>
    void SkipCommit();
}