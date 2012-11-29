using System.Collections.Generic;

namespace Rebus.Timeout
{
    /// <summary>
    /// Abstracts how timeouts are stored and retrieved when they are due.
    /// </summary>
    public interface IStoreTimeouts
    {
        /// <summary>
        /// Should add the specified timeout to the store. An ambient transaction will
        /// be present, so the implementor should enlist if possible.
        /// </summary>
        void Add(Timeout newTimeout);

        /// <summary>
        /// Should get all due timeouts. An ambient transaction will be present, so the implementor should 
        /// enlist if possible. When a timeout has been properly processed, <see cref="DueTimeout.MarkAsProcessed"/>
        /// should be called.
        /// </summary>
        IEnumerable<DueTimeout> GetDueTimeouts();
    }
}