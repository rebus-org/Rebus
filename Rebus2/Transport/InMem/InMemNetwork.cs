using System;
using System.Collections.Concurrent;
using Rebus2.Messages;

namespace Rebus2.Transport.InMem
{
    /// <summary>
    /// Defines a network that the in-mem transport can work on, functioning as a namespace for the queue addresses
    /// </summary>
    public class InMemNetwork
    {
        readonly ConcurrentDictionary<string, ConcurrentQueue<TransportMessage>> _queues = 
            new ConcurrentDictionary<string, ConcurrentQueue<TransportMessage>>(StringComparer.InvariantCultureIgnoreCase);

        public void Deliver(string destinationAddress, TransportMessage msg)
        {
            if (destinationAddress == null) throw new ArgumentNullException("destinationAddress");
            if (msg == null) throw new ArgumentNullException("msg");

            _queues
                .GetOrAdd(destinationAddress, address => new ConcurrentQueue<TransportMessage>())
                .Enqueue(msg);
        }

        public TransportMessage GetNextOrNull(string inputQueueName)
        {
            if (inputQueueName == null) throw new ArgumentNullException("inputQueueName");

            TransportMessage message;

            return _queues
                .GetOrAdd(inputQueueName, address => new ConcurrentQueue<TransportMessage>())
                .TryDequeue(out message)
                ? message
                : null;
        }
    }
}