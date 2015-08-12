using System;
using System.Collections.Generic;

namespace Rebus.Timeout
{
    /// <summary>
    /// Abstracts how timeouts are stored and retrieved when they are due. Please note that implementors are required
    /// to be re-entrant when adding timeouts! Also note that only ONE SINGLE TIMEOUT MANAGER should be using the same timeout storage!
    /// This is because of the transaction, which goes like this: 
    /// 1) retrieve due timeouts
    /// 2) for each due timeout
    /// 3)      send timeout reply
    /// 4)      remove the due timeout from the storage
    /// </summary>
    public interface IStoreTimeouts
    {
        /// <summary>
        /// Should add the specified timeout to the store.
        /// </summary>
        void Add(Timeout newTimeout);

        /// <summary>
        /// Should get all due timeouts. When a timeout has been properly processed, <see cref="DueTimeout.MarkAsProcessed"/>
        /// should be called, which should cause the timeout to be removed from the underlying data store. Also, please remember to
        /// dispose the <see cref="DueTimeoutsResult"/> after the timeouts have been processed.
        /// </summary>
        DueTimeoutsResult GetDueTimeouts();
    }

    /// <summary>
    /// Due timeout result wrapper that allows for returning a sequence of due timeouts along with an action to perform when the timeouts have been processed
    /// </summary>
    public class DueTimeoutsResult : IDisposable
    {
        readonly Action disposeAction;

        /// <summary>
        /// Creates the result wrapper with the given due timeout, optionally including an action to be performed when the result is disposed
        /// </summary>
        public DueTimeoutsResult(IEnumerable<DueTimeout> dueTimeouts, Action disposeAction = null)
        {
            this.disposeAction = disposeAction ?? (() => { });
            DueTimeouts = dueTimeouts;
        }


        /// <summary>
        /// Gets the sequence of due timeout that were returned
        /// </summary>
        public IEnumerable<DueTimeout> DueTimeouts { get; private set; }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            disposeAction();
        }
    }
}