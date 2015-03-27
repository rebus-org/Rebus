using System;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Messages;

namespace Rebus.Transport.InMem
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

        public void CreateQueue(string address)
        {
            _network.CreateQueue(address);
        }

        public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            if (destinationAddress == null) throw new ArgumentNullException("destinationAddress");
            if (message == null) throw new ArgumentNullException("message");
            if (context == null) throw new ArgumentNullException("context");

            if (!_network.HasQueue(destinationAddress))
            {
                throw new ArgumentException(string.Format("Destination queue address '{0}' does not exist!", destinationAddress));
            }

            context.Committed += () =>
            {
                _network.Deliver(destinationAddress, message.ToInMemTransportMessage());
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
                    _network.Deliver(_inputQueueAddress, nextMessage, alwaysQuiet: true);
                };

                return nextMessage.ToTransportMessage();
            }

            await Task.Delay(20);
            
            return null;
        }

        public string Address
        {
            get { return _inputQueueAddress; }
        }
    }
}