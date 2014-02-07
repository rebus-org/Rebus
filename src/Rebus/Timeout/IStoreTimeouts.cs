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
        /// should be called, which should cause the timeout to be removed from the underlying data store.
        /// </summary>
        IEnumerable<DueTimeout> GetDueTimeouts();
    }
}