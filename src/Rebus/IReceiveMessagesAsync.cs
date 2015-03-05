using System.Threading.Tasks;

namespace Rebus
{
    /// <summary>
    /// Interface of something that is capable of receiving messages asynchronously. If no message is available,
    /// null should be returned. If the bus is configured to run with multiple threads, this one
    /// should be reentrant.
    /// </summary>
    public interface IReceiveMessagesAsync : IReceiveMessageBase
    {
        /// <summary>
        /// Attempt to receive the next available message asynchronously. Should return null if no message is available.
        /// </summary>
        Task<ReceivedTransportMessage> ReceiveMessageAsync(ITransactionContext context);
    }
}