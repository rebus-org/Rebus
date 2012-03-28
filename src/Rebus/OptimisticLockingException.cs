using System;
using Rebus.Shared;

namespace Rebus
{
    public class OptimisticLockingException : ApplicationException
    {
        public OptimisticLockingException(ISagaData sagaData)
            : base(string.Format(@"Could not update saga of type {0} with _id {1} _rev {2} because someone else beat us to it",
            sagaData.GetType(), sagaData.Id, sagaData.Revision))
        {
        }

        public OptimisticLockingException(ISagaData sagaData, Exception innerException)
            : base(string.Format(@"Could not update saga of type {0} with _id {1} _rev {2} because someone else beat us to it",
            sagaData.GetType(), sagaData.Id, sagaData.Revision), innerException)
        {
        }
    }

    public class InconsistentTimeToBeReceivedException : ApplicationException
    {
        public InconsistentTimeToBeReceivedException(string message, params object[] objs)
            : base(string.Format(@"When specifying the {0} header, it must be consistent across messages within one batch!

Otherwise, messages might either get deleted before they actually expire, or not expire in time.

{1}", Headers.TimeToBeReceived, string.Format(message, objs)))
        {
            
        }
    }
}