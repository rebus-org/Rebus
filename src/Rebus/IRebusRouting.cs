namespace Rebus
{
    /// <summary>
    /// Groups Rebus' operations for manually routing messages.
    /// </summary>
    public interface IRebusRouting
    {
        /// <summary>
        /// Sends the specified message to the specified destination.
        /// </summary>
        void Send<TCommand>(string destinationEndpoint, TCommand message);

        /// <summary>
        /// Sends a subscription request for <typeparamref name="TEvent"/> to the specified 
        /// destination.
        /// </summary>
        void Subscribe<TEvent>(string publisherInputQueue);

        /// <summary>
        /// Sends the currently handled message to the specified endpoint, preserving all
        /// of the transport level headers.
        /// </summary>
        void ForwardCurrentMessage(string destinationEndpoint);
    }
}