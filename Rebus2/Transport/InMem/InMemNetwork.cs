using System.Collections.Concurrent;
using Rebus2.Messages;

namespace Rebus2.Transport.InMem
{
    public class InMemNetwork
    {
        readonly ConcurrentDictionary<string, ConcurrentQueue<TransportMessage>> _queues = new ConcurrentDictionary<string, ConcurrentQueue<TransportMessage>>();

        public void Deliver(string destinationAddress, TransportMessage msg)
        {
            _queues
                .GetOrAdd(destinationAddress, address => new ConcurrentQueue<TransportMessage>())
                .Enqueue(msg);
        }

        public TransportMessage GetNextOrNull(string inputQueueName)
        {
            TransportMessage message;

            return _queues
                .GetOrAdd(inputQueueName, address => new ConcurrentQueue<TransportMessage>())
                .TryDequeue(out message)
                ? message
                : null;
        }
    }
}