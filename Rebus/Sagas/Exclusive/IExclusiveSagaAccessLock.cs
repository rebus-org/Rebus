using System.Threading;
using System.Threading.Tasks;

namespace Rebus.Sagas.Exclusive
{
    /// <summary>
    /// Defines API for storing sagalocks
    /// </summary>
    public interface IExclusiveSagaAccessLock
    {
        /// <summary>
        /// Aquire a lock for given key
        /// </summary>
        /// <param name="key">Lockingkey</param>
        /// <param name="cancellationToken">Cancellation token which will be cancelled if Rebus shuts down. Can be used if e.g. distributed locks have a timeout associated with them</param>
        /// <returns></returns>
        Task<bool> AquireLockAsync(string key, CancellationToken cancellationToken);
        
        /// <summary>
        /// Release a lock for given key
        /// </summary>
        /// <param name="key">Lockingkey</param>
        /// <returns></returns>
        Task<bool> ReleaseLockAsync(string key);
    }
}