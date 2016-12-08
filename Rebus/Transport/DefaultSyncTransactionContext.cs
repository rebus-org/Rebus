using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Rebus.Bus.Advanced;

namespace Rebus.Transport
{
    /// <summary>
    /// Default implementation of <see cref="ITransactionContext"/>
    /// </summary>
    public class DefaultSyncTransactionContext : ITransactionContext
    {
        readonly TransactionContext _transactionContext = new TransactionContext();

        /// <summary>
        /// Stash of items that can carry stuff for later use in the transaction
        /// </summary>
        public ConcurrentDictionary<string, object> Items => _transactionContext.Items;

        /// <summary>
        /// Registers a listener to be called when the queue transaction is committed. This hook is reserved for the queue transaction
        /// and you may get unpredictable results of you enlist your own transaction in this
        /// </summary>
        public void OnCommitted(Func<Task> commitAction) => _transactionContext.OnCommitted(commitAction);

        /// <summary>
        /// Registers a listener to be called AFTER the queue transaction has been successfully committed (i.e. all listeners
        /// registered with <see cref="ITransactionContext.OnCommitted"/> have been executed). This would be a good place to complete the incoming
        /// message.
        /// </summary>
        public void OnCompleted(Func<Task> completedAction) => _transactionContext.OnCompleted(completedAction);

        /// <summary>
        /// Registers a listener to be called when the queue transaction is aborted. This hook is reserved for the queue transaction
        /// and you may get unpredictable results of you enlist your own transaction in this
        /// </summary>
        public void OnAborted(Action abortedAction) => _transactionContext.OnAborted(abortedAction);

        /// <summary>
        /// Registers a listener to be called after the transaction is over
        /// </summary>
        public void OnDisposed(Action disposedAction) => _transactionContext.OnDisposed(disposedAction);

        /// <summary>
        /// Indicates that the transaction must not be committed and commit handlers must not be run
        /// </summary>
        public void Abort() => _transactionContext.Abort();

        /// <summary>
        /// Executes commit actions enlisted in the transaction with <see cref="ITransactionContext.OnCommitted"/>
        /// </summary>
        public Task Commit() => _transactionContext.Commit();

        /// <summary>
        /// Performs the registered cleanup actions. If the transaction has not been committed, it will be aborted before the cleanup happens.
        /// </summary>
        public void Dispose() => _transactionContext.Dispose();

        /// <summary>
        /// Ends the current transaction by either committing it or aborting it, depending on whether someone voted for abortion
        /// </summary>
        public void Complete()
        {
            AsyncHelpers.RunSync(_transactionContext.Complete);
        }
    }
}