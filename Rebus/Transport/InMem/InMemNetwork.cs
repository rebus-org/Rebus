using System;
using System.Collections.Concurrent;
using System.Threading;
using Rebus.Extensions;
using Rebus.Messages;

namespace Rebus.Transport.InMem
{
    /// <summary>
    /// Defines a network that the in-mem transport can work on, functioning as a namespace for the queue addresses
    /// </summary>
    public class InMemNetwork
    {
        static int _networkIdCounter;

        readonly string _networkId = string.Format("In-mem network {0}", Interlocked.Increment(ref _networkIdCounter));

        readonly ConcurrentDictionary<string, ConcurrentQueue<TransportMessage>> _queues = 
            new ConcurrentDictionary<string, ConcurrentQueue<TransportMessage>>(StringComparer.InvariantCultureIgnoreCase);

        readonly bool _outputEventsToConsole;

        public InMemNetwork(bool outputEventsToConsole = false)
        {
            _outputEventsToConsole = outputEventsToConsole;

            if (_outputEventsToConsole)
            {
                Console.WriteLine("Created in-mem network '{0}'", _networkId);
            }
        }

        public void Deliver(string destinationAddress, TransportMessage msg)
        {
            if (destinationAddress == null) throw new ArgumentNullException("destinationAddress");
            if (msg == null) throw new ArgumentNullException("msg");

            if (_outputEventsToConsole)
            {
                Console.WriteLine("{0} -> {1} ({2})", msg.Headers.GetValueOrNull(Headers.MessageId) ?? "<no message ID>", destinationAddress, _networkId);
            }

            var messageQueue = _queues
                .GetOrAdd(destinationAddress, address => new ConcurrentQueue<TransportMessage>());

            messageQueue.Enqueue(msg);
        }

        public TransportMessage GetNextOrNull(string inputQueueName)
        {
            if (inputQueueName == null) throw new ArgumentNullException("inputQueueName");

            TransportMessage message;

            var messageQueue = _queues.GetOrAdd(inputQueueName, address => new ConcurrentQueue<TransportMessage>());

            if (!messageQueue.TryDequeue(out message)) return null;

            if (_outputEventsToConsole)
            {
                Console.WriteLine("{0} -> {1} ({2})", inputQueueName, message.Headers.GetValueOrNull(Headers.MessageId) ?? "<no message ID>", _networkId);
            }

            return message;
        }

        public bool HasQueue(string address)
        {
            return _queues.ContainsKey(address);
        }

        public void CreateQueue(string address)
        {
            _queues.TryAdd(address, new ConcurrentQueue<TransportMessage>());
        }
    }
}