using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Transport
{
    public interface ITransport
    {
        void CreateQueue(string address);
        Task Send(string destinationAddress, TransportMessage msg, ITransactionContext context);
        Task<TransportMessage> Receive(ITransactionContext context);
        string Address { get; }
    }
}