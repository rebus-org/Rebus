using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Rebus.Transport;

/// <summary>
/// Represents "a transaction", which can be either:
/// 
/// (a) a unit of work where one or more messages get sent/published, or
/// (b) a unit of work where a message is received, one or more messages get sent/published, and the received messages gets ACKed
///
/// The sequence is
///
/// (1) Receive incoming message
/// (2) Call user code (i.e. handlers and such)
/// (3) Commit
/// (4) ACK
///
/// where steps 1, 2, and 4 are simply omitted if we're talking about scenario (a).
/// </summary>
public interface ITransactionContext : IDisposable
{
    /// <summary>
    /// Stash of items that can carry stuff for later use in the transaction
    /// </summary>
    ConcurrentDictionary<string, object> Items { get; }

    /// <summary>
    /// Registers a listener to be called when the queue transaction is committed.
    /// This is a good place to enlist whatever work needs to be done when the message transaction is about to be completed.
    /// </summary>
    void OnCommit(Func<ITransactionContext, Task> commitAction);

    /// <summary>
    /// Registers a listener to be called when the queue transaction is rolled back.
    /// This is a good place to enlist whatever rollback work needs to be done when the message transaction is about to be rolled back.
    /// </summary>
    void OnRollback(Func<ITransactionContext, Task> abortedAction);

    /// <summary>
    /// Registers a callback to be invoked as an "ACK handler", which is what finishes a message transaction.
    /// </summary>
    void OnAck(Func<ITransactionContext, Task> completedAction);

    /// <summary>
    /// Registers a callback to be invoked as a "NACK handler", which is what finishes a message transaction.
    /// </summary>
    void OnNack(Func<ITransactionContext, Task> commitAction);

    /// <summary>
    /// Registers a listener to be called after the transaction is over
    /// </summary>
    void OnDisposed(Action<ITransactionContext> disposedAction);

    /// <summary>
    /// Sets the result of the transaction.
    /// If <paramref name="commit"/> is true, all commit callbacks registered with <see cref="OnCommit"/> will be invoked. If false, rollback callbacks registered with <see cref="OnRollback"/> will be invoked.
    /// If <paramref name="ack"/> is true, all ACK callbacks registered with <see cref="OnAck"/> will be invoked. If false, NACK callbacks registered with <see cref="OnNack"/> will be invoked. 
    /// </summary>
    void SetResult(bool commit, bool ack);
}