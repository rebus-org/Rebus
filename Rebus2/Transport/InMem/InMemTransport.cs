using System;
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
            if (network == null) throw new ArgumentNullException("network");
            if (inputQueueAddress == null) throw new ArgumentNullException("inputQueueAddress");

            _network = network;
            _inputQueueAddress = inputQueueAddress;

            _network.CreateQueue(inputQueueAddress);
        }

        public async Task Send(string destinationAddress, TransportMessage msg, ITransactionContext context)
        {
            if (destinationAddress == null) throw new ArgumentNullException("destinationAddress");
            if (msg == null) throw new ArgumentNullException("msg");
            if (context == null) throw new ArgumentNullException("context");

            if (!_network.HasQueue(destinationAddress))
            {
                throw new ArgumentException(string.Format("Destination queue address '{0}' does not exist!", destinationAddress));
            }

            context.Committed += () =>
            {
                _network.Deliver(destinationAddress, msg);
            };
        }

        public async Task<TransportMessage> Receive(ITransactionContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            var nextMessage = _network.GetNextOrNull(_inputQueueAddress);
            
            if (nextMessage != null)
            {
                context.Aborted += () =>
                {
                    _network.Deliver(_inputQueueAddress, nextMessage);
                };

                return nextMessage;
            }

            await Task.Delay(200);
            
            return null;
        }

        public string Address
        {
            get { return _inputQueueAddress; }
        }
    }
}