using System;

namespace Rebus
{
    /// <summary>
    /// Defines a context of a transaction, allowing transports to hook operations up on transaction events
    /// </summary>
    public interface ITransactionContext : IDisposable
    {
        /// <summary>
        /// Indicates whether the current context is actually transactional. If it is not transactional,
        /// the dictionary will be very short-lived, and you should not subscribe to any events.
        /// </summary>
        bool IsTransactional { get; }

        /// <summary>
        /// Gives access to a dictionary of stuff that will be kept for the duration of the transaction.
        /// </summary>
        object this[string key] { get; set; }

        /// <summary>
        /// Will be raised when it is time to commit the transaction. The transport should do its final
        /// commit work when this event is raised.
        /// </summary>
        event Action DoCommit;

        /// <summary>
        /// Will be raised in the event that the transaction should be rolled back.
        /// </summary>
        event Action DoRollback;

        /// <summary>
        /// Will be raised before doing the actual commit
        /// </summary>
        event Action BeforeCommit;

        /// <summary>
        /// Will be raised after a transaction has been rolled back
        /// </summary>
        event Action AfterRollback;

        /// <summary>
        /// Will be raised after all work is done, allowing you to clean up resources etc.
        /// </summary>
        event Action Cleanup;
    }
}