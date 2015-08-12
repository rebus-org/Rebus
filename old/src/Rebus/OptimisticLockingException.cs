using System;
using System.Runtime.Serialization;

namespace Rebus
{
    /// <summary>
    /// Exception that gets thrown in cases where a race condition is detected for a piece of saga data.
    /// </summary>
    [Serializable]
    public class OptimisticLockingException : ApplicationException
    {
        /// <summary>
        /// Constructs the exception with an error message that explains how a race condition was detected on the specified saga data
        /// </summary>
        public OptimisticLockingException(ISagaData sagaData)
            : base(string.Format(@"Could not update saga of type {0} with _id {1} _rev {2} because someone else beat us to it",
            sagaData.GetType(), sagaData.Id, sagaData.Revision))
        {
        }

        /// <summary>
        /// Constructs the exception with an error message that explains how a race condition was detected on the specified saga data,
        /// supplying as extra information a caught exception
        /// </summary>
        public OptimisticLockingException(ISagaData sagaData, Exception innerException)
            : base(string.Format(@"Could not update saga of type {0} with _id {1} _rev {2} because someone else beat us to it",
            sagaData.GetType(), sagaData.Id, sagaData.Revision), innerException)
        {
        }

        /// <summary>
        /// Ctor necessary for serialization
        /// </summary>
        public OptimisticLockingException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }
    }
}