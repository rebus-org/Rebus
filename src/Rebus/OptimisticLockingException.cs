using System;

namespace Rebus
{
    /// <summary>
    /// Exception that gets thrown in cases where a race condition is detected for a piece of saga data.
    /// </summary>
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