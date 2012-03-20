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
        /// Should get all due timeouts and remove them at the same time. An ambient transaction
        /// will be present, so the implementor should enlist if possible.
        /// </summary>
        IEnumerable<Timeout> RemoveDueTimeouts();
    }
}