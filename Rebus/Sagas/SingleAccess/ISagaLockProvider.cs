using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rebus.Sagas.SingleAccess
{
	/// <summary>
	/// Interface for providing a lock on a saga
	/// </summary>
    public interface ISagaLockProvider
	{
		/// <summary>
		/// Acquire a <seealso cref="ISagaLock"/> that can provide mutual exclusion semantics to a given saga
		/// </summary>
		/// <param name="sagaCorrelationId">The correlation identifier of the saga the lock is requested for. Implementors would usually use this to key a lock specific to this saga</param>
		/// <returns>A <seealso cref="ISagaLock"/> that can be acquired for this saga</returns>
	    Task<ISagaLock> LockFor(object sagaCorrelationId);
    }
}
