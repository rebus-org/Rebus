using System.Threading.Tasks;

namespace Rebus.Sagas.Exclusive
{
    /// <summary>
    /// Defines API for storing sagalocks
    /// </summary>
    public interface IHandleSagaExlusiveLock
    {
        /// <summary>
        /// Aquire a lock for given key
        /// </summary>
        /// <param name="key">Lockingkey</param>
        /// <returns></returns>
        Task<bool> AquireLockAsync(string key);
        /// <summary>
        /// Release a lock for given key
        /// </summary>
        /// <param name="key">Lockingkey</param>
        /// <returns></returns>
        Task<bool> ReleaseLockAsync(string key);

    }
}