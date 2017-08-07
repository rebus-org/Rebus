using System.Threading.Tasks;

namespace Rebus.Sagas.SingleAccess
{
	/// <summary>
	/// An implementation of <seealso cref="ISagaLockProvider"/> which uses a machine wide <seealso cref="System.Threading.Semaphore"/>
	/// <remarks>Note: This will only provide single access saga protection for a single machine. If there are multiple worker machines you will need to use a distributed locking system</remarks>
	/// </summary>
    public class SemaphoreSagaLockProvider : ISagaLockProvider
    {
	    /// <summary>
	    /// Acquire a <seealso cref="ISagaLock"/> that can provide mutual exclusion semantics to a given saga
	    /// </summary>
	    /// <param name="sagaCorrelationId">The correlation identifier of the saga the lock is requested for. Implementors would usually use this to key a lock specific to this saga</param>
	    /// <returns>A <seealso cref="ISagaLock"/> that can be acquired for this saga</returns>
	    public Task<ISagaLock> LockFor(object sagaCorrelationId)
		{
		    return Task.FromResult<ISagaLock>(new SemaphoreSagaLock($"rbs2-saga-lock-{sagaCorrelationId}"));
	    }
	}
}
