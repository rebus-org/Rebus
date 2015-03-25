using System;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.AzureServiceBus
{
    public class AzureServiceBusTransport : ITransport
    {
        readonly string _inputQueueAddress;

        public AzureServiceBusTransport(string inputQueueAddress)
        {
            _inputQueueAddress = inputQueueAddress;
        }

        public void CreateQueue(string address)
        {
            throw new NotImplementedException();
        }

        public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            throw new NotImplementedException();
        }

        public async Task<TransportMessage> Receive(ITransactionContext context)
        {
            throw new NotImplementedException();
        }

        public string Address
        {
            get { return _inputQueueAddress; }
        }
    }
}
