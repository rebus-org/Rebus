using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        readonly ConcurrentDictionary<string, ConcurrentQueue<InMemTransportMessage>> _queues =
            new ConcurrentDictionary<string, ConcurrentQueue<InMemTransportMessage>>(StringComparer.InvariantCultureIgnoreCase);

        readonly bool _outputEventsToConsole;

        public InMemNetwork(bool outputEventsToConsole = false)
        {
            _outputEventsToConsole = outputEventsToConsole;

            if (_outputEventsToConsole)
            {
                Console.WriteLine("Created in-mem network '{0}'", _networkId);
            }
        }

        public void Reset()
        {
            Console.WriteLine("Resetting in-mem network '{0}'", _networkId);
            _queues.Clear();
        }

        public void Deliver(string destinationAddress, InMemTransportMessage msg, bool alwaysQuiet = false)
        {
            if (destinationAddress == null) throw new ArgumentNullException("destinationAddress");
            if (msg == null) throw new ArgumentNullException("msg");

            if (_outputEventsToConsole && !alwaysQuiet)
            {
                Console.WriteLine("{0} ---> {1} ({2})", msg.Headers.GetValueOrNull(Headers.MessageId) ?? "<no message ID>", destinationAddress, _networkId);
            }

            var messageQueue = _queues
                .GetOrAdd(destinationAddress, address => new ConcurrentQueue<InMemTransportMessage>());

            messageQueue.Enqueue(msg);
        }

        public InMemTransportMessage GetNextOrNull(string inputQueueName)
        {
            if (inputQueueName == null) throw new ArgumentNullException("inputQueueName");

            InMemTransportMessage message;

            var messageQueue = _queues.GetOrAdd(inputQueueName, address => new ConcurrentQueue<InMemTransportMessage>());

            if (!messageQueue.TryDequeue(out message)) return null;

            if (MessageIsExpired(message))
            {
                Console.WriteLine("{0} EXP> {1} ({2})", inputQueueName, message.Headers.GetValueOrNull(Headers.MessageId) ?? "<no message ID>", _networkId);
                return null;
            }

            if (_outputEventsToConsole)
            {
                Console.WriteLine("{0} ---> {1} ({2})", inputQueueName, message.Headers.GetValueOrNull(Headers.MessageId) ?? "<no message ID>", _networkId);
            }

            return message;
        }

        bool MessageIsExpired(InMemTransportMessage message)
        {
            var headers= message.Headers;
            if (!headers.ContainsKey(Headers.TimeToBeReceived)) return false;

            var timeToBeReceived = headers[Headers.TimeToBeReceived];
            var maximumAge = TimeSpan.Parse(timeToBeReceived);

            return message.Age > maximumAge;
        }

        public bool HasQueue(string address)
        {
            return _queues.ContainsKey(address);
        }

        public void CreateQueue(string address)
        {
            _queues.TryAdd(address, new ConcurrentQueue<InMemTransportMessage>());
        }
    }
}