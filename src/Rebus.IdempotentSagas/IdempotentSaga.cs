using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rebus.IdempotentSagas
{
    /// <summary>
    /// A saga data that is able to keep track of its processed messages.
    /// </summary>
    public interface IIdempotentSagaData : ISagaData
    {
        /// <summary>
        /// The processed messages for this saga.
        /// </summary>
        IList<IdempotentSagaResults> ExecutionResults { get; set; }
    }

    /// <summary>
    /// A saga that keeps track of its processed messages.
    /// </summary>
    public abstract class IdempotentSaga<TData> : Saga<TData>
        where TData : IIdempotentSagaData
    {
    }
}
