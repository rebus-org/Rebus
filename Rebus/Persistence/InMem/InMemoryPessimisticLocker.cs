using System.Collections.Concurrent;
using System.Threading.Tasks;
using Rebus.Sagas.Locking;
#pragma warning disable 1998

namespace Rebus.Persistence.InMem
{
    /// <summary>
    /// In-memory implementation of <see cref="IPessimisticLocker"/> that can be shared among multiple bus instances
    /// if necessary. Can be used when only endpoints in the same process are accessing saga data instances that need
    /// to be locked as a lightweight locking mechanis.
    /// Implements its locking simply by using a <see cref="ConcurrentDictionary{TKey,TValue}"/>
    /// </summary>
    public class InMemoryPessimisticLocker : IPessimisticLocker
    {
        readonly ConcurrentDictionary<string, object> _locks = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Attempts to grab the lock with the specified ID. Returns true if it was successfully grabbed,
        /// false otherwise.
        /// </summary>
        public async Task<bool> TryAcquire(string lockId)
        {
            return _locks.TryAdd(lockId, new object());
        }

        /// <summary>
        /// Releases the lock with the specified ID.
        /// </summary>
        public async Task Release(string lockId)
        {
            object dummy;
            _locks.TryRemove(lockId, out dummy);
        }

        /// <summary>
        /// Gets the number of locks currently acquired
        /// </summary>
        public int Count => _locks.Count;
    }
}