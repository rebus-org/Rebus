using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rebus.IdempotentSagas
{
    /// <summary>
    /// Contains keys of headers with special meaning in Rebus.IdempotentSagas.
    /// </summary>
    public class Headers
    {
        /// <summary>
        /// Contains the original id of the message to be replayed.
        /// </summary>
        public const string OriginalMessageId = "idempotent-saga-original-msg-id";

        /// <summary>
        /// 
        /// </summary>
        public const string IdempotentSagaResults = "idempotent-saga-results";
    }
}
