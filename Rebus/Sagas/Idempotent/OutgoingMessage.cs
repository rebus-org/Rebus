using System.Collections.Generic;
using Rebus.Messages;

namespace Rebus.Sagas.Idempotent
{
    public class OutgoingMessage
    {
        public OutgoingMessage(IEnumerable<string> destinationAddresses, TransportMessage transportMessage)
        {
            DestinationAddresses = destinationAddresses;
            TransportMessage = transportMessage;
        }

        public IEnumerable<string> DestinationAddresses { get; private set; }
        
        public TransportMessage TransportMessage { get; private set; }
    }
}