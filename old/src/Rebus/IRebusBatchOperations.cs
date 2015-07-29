using System;
using System.Collections;

namespace Rebus
{
    /// <summary>
    /// Groups the batch operations that Rebus can perform.
    /// </summary>
    public interface IRebusBatchOperations
    {
        /// <summary>
        /// Sends the specified batch of messages, dividing the batch into batches
        /// for individual recipients if necessary. For each recipient, the order
        /// of the messages within the batch is preserved.
        /// </summary>
        [Obsolete(ObsoleteWarning.BatchOpsDeprecated)]
        void Send(IEnumerable messages);

        /// <summary>
        /// Publishes the specified batch of messages, dividing the batch into
        /// batches for individual recipients if necessary. For each subscriber,
        /// the order of the messages within the batch is preserved.
        /// </summary>
        [Obsolete(ObsoleteWarning.BatchOpsDeprecated)]
        void Publish(IEnumerable messages);

        /// <summary>
        /// Sends a batch of replies back to the sender of the message currently being handled.
        /// Can only be called when a <see cref="MessageContext"/> has been established, which happens
        /// during the handling of an incoming message.
        /// </summary>
        [Obsolete(ObsoleteWarning.BatchOpsDeprecated)]
        void Reply(IEnumerable messages);
    }
}