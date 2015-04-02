using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rebus.Transport
{
    /// <summary>
    /// Represents the context of one queue transaction
    /// </summary>
    public interface ITransactionContext : IDisposable
    {
        /// <summary>
        /// Stash of items that can carry stuff for later use in the transaction
        /// </summary>
        Dictionary<string, object> Items { get; }

        /// <summary>
        /// Registers a listener to be called when the queue transaction is committed. This hook is reserved for the queue transaction
        /// and you may get unpredictable results of you enlist your own transaction in this
        /// </summary>
        void OnCommitted(Func<Task> commitAction);

        /// <summary>
        /// Registers a listener to be called when the queue transaction is aborted. This hook is reserved for the queue transaction
        /// and you may get unpredictable results of you enlist your own transaction in this
        /// </summary>
        void OnAborted(Action abortedAction);
        
        /// <summary>
        /// Registers a listener to be called after the transaction is over
        /// </summary>
        void OnDisposed(Action disposedAction);
   
        /// <summary>
        /// Signals that something is wrong and the queue transaction must be aborted
        /// </summary>
        void Abort();
    }
}