using System.Threading.Tasks;
using Rebus2.Messages;

namespace Rebus2.Transport.InMem
{
    public class InMemTransport : ITransport
    {
        readonly InMemNetwork _network;
        readonly string _inputQueueAddress;

        public InMemTransport(InMemNetwork network, string inputQueueAddress)
        {
            _network = network;
            _inputQueueAddress = inputQueueAddress;
        }

        public async Task Send(string destinationAddress, TransportMessage msg, ITransactionContext context)
        {
            context.Committed += () =>
            {
                _network.Deliver(destinationAddress, msg);
            };
        }

        public async Task<TransportMessage> Receive(ITransactionContext context)
        {
            var nextMessage = _network.GetNextOrNull(_inputQueueAddress);
            if (nextMessage != null) return nextMessage;

            await Task.Delay(200);
            
            return null;
        }

        public string Address
        {
            get { return _inputQueueAddress; }
        }
    }
}