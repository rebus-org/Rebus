using System.Threading;
using System.Threading.Tasks;

namespace Rebus.ExclusiveLocks;

/// <summary>
/// Defines API for generic exclusive locks
/// </summary>
public interface IExclusiveAccessLock
{
    /// <summary>
    /// Acquire a lock for given key
    /// </summary>
    /// <param name="key">Locking key</param>
    /// <param name="cancellationToken">Cancellation token which will be cancelled if Rebus shuts down. Can be used if e.g. distributed locks have a timeout associated with them</param>
    /// <returns>True if the lock was acquired, false if not</returns>
    Task<bool> AcquireLockAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Determines if a lock has been acquired or not
    /// </summary>
    /// <param name="key">Locking key</param>
    /// <param name="cancellationToken">Cancellation token which will be cancelled if Rebus shuts down. Can be used if e.g. distributed locks have a timeout associated with them</param>
    /// <returns>True of the lock was acquired already, false if not</returns>
    Task<bool> IsLockAcquiredAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Release a lock for given key
    /// </summary>
    /// <param name="key">Locking key</param>
    /// <returns>True of the lock was released, false if not</returns>
    Task<bool> ReleaseLockAsync(string key);
}