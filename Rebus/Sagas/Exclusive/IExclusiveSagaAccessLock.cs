using System.Threading;
using System.Threading.Tasks;

namespace Rebus.Sagas.Exclusive
{
    /// <summary>
    /// Defines API for storing saga locks
    /// </summary>
    public interface IExclusiveSagaAccessLock
    {
        /// <summary>
        /// Aquire a lock for given key
        /// </summary>
        /// <param name="key">Locking key</param>
        /// <param name="cancellationToken">Cancellation token which will be cancelled if Rebus shuts down. Can be used if e.g. distributed locks have a timeout associated with them</param>
        /// <returns>True if the lock was aquired, false if not</returns>
        Task<bool> AquireLockAsync(int key, CancellationToken cancellationToken);

        /// <summary>
        /// Release a lock for given key
        /// </summary>
        /// <param name="key">Locking key</param>
        /// <returns>True if the lock was released, false if not</returns>
        Task<bool> ReleaseLockAsync(int key);
    }
}