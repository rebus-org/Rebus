using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rebus.Sagas;

namespace Rebus.Sagas.SingleAccess
{
	/// <summary>
	/// Marker interface used to identify a <seealso cref="Saga{TSagaData}"/> implementation as requiring single handling across all workers
	/// </summary>
    public interface ISingleAccessSaga
    {
    }
}
