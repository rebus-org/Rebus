using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rebus.Sagas;

namespace Rebus.Sagas.SingleAccess
{
	/// <summary>
	/// Defines an interface for acquiring a lock on a <seealso cref="Saga{TSagaData}"/>. Implementors should release the lock, if acquired, on disposal
	/// </summary>
    public interface ISagaLock : IDisposable
	{
		/// <summary>
		/// Attempt to acquire a lock. If the lock was successfully acquired return <c>true</c>. If the lock could not be acquired returns <c>false</c>
		/// </summary>
	    Task<bool> TryAcquire();
    }
}
