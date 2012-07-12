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
        void Send(params object[] messages);

        /// <summary>
        /// Publishes the specified batch of messages, dividing the batch into
        /// batches for individual recipients if necessary. For each subscriber,
        /// the order of the messages within the batch is preserved.
        /// </summary>
        void Publish(params object[] messages);
    }
}