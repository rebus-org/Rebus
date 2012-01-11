namespace Rebus
{
    /// <summary>
    /// Interface of something that is capable of sending a <see cref="TransportMessageToSend"/> somewhere.
    /// </summary>
    public interface ISendMessages
    {
        /// <summary>
        /// Sends the specified <see cref="TransportMessageToSend"/> to the endpoint
        /// with the supplied queue name.
        /// </summary>
        void Send(string destinationQueueName, TransportMessageToSend message);
    }
}