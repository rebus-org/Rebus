using System;
using System.Runtime.Serialization;

namespace Rebus.Bus
{
    /// <summary>
    /// Special exception that wraps an exception that occurred while committing the current unit of work
    /// </summary>
    [Serializable]
    public class UnitOfWorkCommitException : ApplicationException
    {
        /// <summary>
        /// Mandatory exception ctor
        /// </summary>
        protected UnitOfWorkCommitException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Constructs the unit of work commit exception with a message pointing to the unit of work that could not be committed,
        /// wrapping as the inner exception the exception caught on commit
        /// </summary>
        public UnitOfWorkCommitException(Exception innerException, IUnitOfWork unitOfWork)
            : base(string.Format("An exception occurred while attempting to commit the unit of work {0}", unitOfWork), innerException)
        {
        }
    }
}