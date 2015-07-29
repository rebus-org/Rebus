namespace Rebus
{
    /// <summary>
    /// Interface of something that is capable of sending a <see cref="TransportMessageToSend"/> somewhere.
    /// </summary>
    public interface ISendMessages
    {
        /// <summary>
        /// Sends the specified <see cref="TransportMessageToSend"/> to the endpoint with the specified input queue name,
        /// enlisting in the specified <see cref="ITransactionContext"/>.
        /// </summary>
        void Send(string destination, TransportMessageToSend message, ITransactionContext context);
    }
}