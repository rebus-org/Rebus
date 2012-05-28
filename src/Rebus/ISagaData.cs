using System;

namespace Rebus
{
    public interface ISagaData
    {
        /// <summary>
        /// This is the ID of the saga. It should be set in the saga data, e.g. in the constructor
        /// of the class implementing this interface, ensuring that has been set when the saga
        /// is persisted the first time.
        /// </summary>
        Guid Id { get; set; }
        
        /// <summary>
        /// This is the revision of this saga. It may be used by the saga persister to implement
        /// optimistic locking. Not all saga persisters need to do this though.
        /// </summary>
        int Revision { get; set; }
    }
}