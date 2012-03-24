using System;

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
}