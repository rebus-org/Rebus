using System.Threading.Tasks;
using Rebus2.Messages;

namespace Rebus2.Transport
{
    public interface ITransport
    {
        Task Send(string destinationAddress, TransportMessage msg, ITransactionContext context);
        Task<TransportMessage> Receive(ITransactionContext context);
    }
}