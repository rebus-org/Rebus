using System.Threading.Tasks;

namespace Rebus.Sagas.Locking
{
    /// <summary>
    /// Defines contract of a very simple pessimistic lock
    /// </summary>
    public interface IPessimisticLock
    {
        /// <summary>
        /// Attempts to grab the lock with the specified ID. Returns true if it was successfully grabbed,
        /// false otherwise.
        /// </summary>
        Task<bool> TryAcquire(string lockId);

        /// <summary>
        /// Releases the lock with the specified ID.
        /// </summary>
        Task Release(string lockid);
    }
}