namespace Rebus
{
    /// <summary>
    /// Extends the capabilities of <see cref="IBus"/> with some more advanced features.
    /// </summary>
    public interface IAdvancedBus : IBus
    {
        /// <summary>
        /// Sends the specified message to the specified destination.
        /// </summary>
        void Send<TCommand>(string endpoint, TCommand message);

        /// <summary>
        /// Sends a subscription request for <typeparamref name="TEvent"/> to the specified 
        /// destination.
        /// </summary>
        void Subscribe<TEvent>(string publisherInputQueue);

        /// <summary>
        /// Sends the specified batch of messages, dividing the batch into bacthes
        /// for individual recipients if necessary. For each recipient, the order
        /// of the messages within the batch is preserved.
        /// </summary>
        void SendBatch(params object[] messages);

        /// <summary>
        /// Publishes the specified batch of messages, dividing the batch into
        /// batches for individual recipients if necessary. For each subscriber,
        /// the order of the messages within the batch is preserved.
        /// </summary>
        void PublishBatch(params object[] messages);

        /// <summary>
        /// Gives access to all the different event hooks that Rebus exposes.
        /// </summary>
        IRebusEvents Events { get; }
    }
}