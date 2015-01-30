using System.Threading.Tasks;

namespace Rebus
{
    /// <summary>
    /// Interface of something that is capable of sending a <see cref="TransportMessageToSend"/> somewhere asynchronously.
    /// </summary>
    public interface ISendMessagesAsync
    {
        /// <summary>
        /// Asynchronously sends the specified <see cref="TransportMessageToSend"/> to the endpoint with
        /// the specified input queue name, enlisting in the specified <see cref="ITransactionContext"/>.
        /// </summary>
        Task SendAsync(string destination, TransportMessageToSend message, ITransactionContext context);
    }
}