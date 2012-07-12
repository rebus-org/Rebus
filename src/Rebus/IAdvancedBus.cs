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
        /// Gives access to all the different event hooks that Rebus exposes.
        /// </summary>
        IRebusEvents Events { get; }

        /// <summary>
        /// Gives access to Rebus' batch operations.
        /// </summary>
        IRebusBatchOperations Batch { get; }
    }
}