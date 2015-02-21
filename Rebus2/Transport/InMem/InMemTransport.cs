using System.Threading.Tasks;
using Rebus2.Messages;

namespace Rebus2.Transport.InMem
{
    public class InMemTransport : ITransport
    {
        readonly InMemNetwork _network;
        readonly string _inputQueueName;

        public InMemTransport(InMemNetwork network, string inputQueueName)
        {
            _network = network;
            _inputQueueName = inputQueueName;
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
            var nextMessage = _network.GetNextOrNull(_inputQueueName);
            if (nextMessage != null) return nextMessage;

            await Task.Delay(200);
            
            return null;
        }

        public string Address
        {
            get { return _inputQueueName; }
        }
    }
}