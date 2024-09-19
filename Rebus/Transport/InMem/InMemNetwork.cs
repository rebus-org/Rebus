using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;

namespace Rebus.Transport.InMem;

/// <summary>
/// Defines a network that the in-mem transport can work on, functioning as a namespace for the queue addresses
/// </summary>
public class InMemNetwork
{
    static int _networkIdCounter;

    readonly string _networkId = $"In-mem network {Interlocked.Increment(ref _networkIdCounter)}";
    readonly ConcurrentDictionary<string, ConcurrentQueue<InMemTransportMessage>> _queues = new(StringComparer.OrdinalIgnoreCase);
    readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _subscribers = new(StringComparer.OrdinalIgnoreCase);

    readonly ILog _log;

    /// <summary>
    /// Retrieves all queues.
    /// </summary>
    public IEnumerable<string> Queues => _queues.Keys;

    /// <summary>
    /// Constructs the in-mem network, optionally (if <paramref name="outputEventsToConsole"/> is set to true) outputting information
    /// about what is happening inside it to <see cref="Console.Out"/>
    /// </summary>
    public InMemNetwork(bool outputEventsToConsole = false)
        : this(outputEventsToConsole ? new ConsoleLoggerFactory(colored: false) : null)
    {
    }

    /// <summary>
    /// Constructs the in-mem network, outputting information about what is happening inside it to the given logger.
    /// </summary>
    public InMemNetwork(IRebusLoggerFactory loggerFactory)
    {
        loggerFactory ??= new NullLoggerFactory();

        _log = loggerFactory.GetLogger<InMemNetwork>();
        _log.Info($"Created in-mem network '{_networkId}'");
    }

    /// <summary>
    /// Resets the network (i.e. all queues and their messages are deleted)
    /// </summary>
    public void Reset()
    {
        _log.Info($"Resetting in-mem network '{_networkId}'");

        _queues.Clear();
        _subscribers.Clear();
    }

    /// <summary>
    /// Get the total count of all queue messages
    /// </summary>
    public int Count()
    {
        return _queues.Values.Sum(q => q.Count);
    }

    /// <summary>
    /// Get the current queue message count of the specified <paramref name="inputQueueName"/>
    /// </summary>
    public int Count(string inputQueueName)
    {
        if (inputQueueName == null) throw new ArgumentNullException(nameof(inputQueueName));

        var messageQueue = _queues.GetOrAdd(inputQueueName, _ => new ConcurrentQueue<InMemTransportMessage>());

        return messageQueue.Count;
    }

    /// <summary>
    /// Delivers the specified <see cref="InMemTransportMessage"/> to the address specified by <paramref name="destinationAddress"/>.
    /// If <paramref name="alwaysQuiet"/> is set to true, no events will ever be printed to <see cref="Console.Out"/>
    /// (can be used by an in-mem transport to return a message to a queue, as if there was a queue transaction that was rolled back)
    /// </summary>
    public void Deliver(string destinationAddress, InMemTransportMessage msg, bool alwaysQuiet = false)
    {
        if (destinationAddress == null) throw new ArgumentNullException(nameof(destinationAddress));
        if (msg == null) throw new ArgumentNullException(nameof(msg));

        if (!alwaysQuiet)
        {
            var messageId = msg.Headers.GetValueOrNull(Headers.MessageId) ?? "<no message ID>";

            _log.Info("{messageId} ---> {destinationAddress} ({_networkId})", messageId, destinationAddress, _networkId);
        }

        _queues.GetOrAdd(destinationAddress, _ => new ConcurrentQueue<InMemTransportMessage>()).Enqueue(msg);
    }

    /// <summary>
    /// Gets the next message from the queue with the given <paramref name="inputQueueName"/>, returning null if no messages are available.
    /// </summary>
    public InMemTransportMessage GetNextOrNull(string inputQueueName)
    {
        if (inputQueueName == null) throw new ArgumentNullException(nameof(inputQueueName));

        var messageQueue = _queues.GetOrAdd(inputQueueName, address => new ConcurrentQueue<InMemTransportMessage>());

        while (true)
        {
            if (!messageQueue.TryDequeue(out var message)) return null;

            var messageId = message.Headers.GetValueOrNull(Headers.MessageId) ?? "<no message ID>";

            if (MessageIsExpired(message))
            {
                _log.Info($"{inputQueueName} EXPIRED> {messageId} ({_networkId})");
                continue;
            }

            _log.Info($"{inputQueueName} ---> {messageId} ({_networkId})");

            return message;
        }
    }

    /// <summary>
    /// Returns whether the network has a queue with the specified name
    /// </summary>
    public bool HasQueue(string address) => _queues.ContainsKey(address);

    /// <summary>
    /// Creates a queue on the network with the specified name
    /// </summary>
    public void CreateQueue(string address) => _queues.TryAdd(address, new ConcurrentQueue<InMemTransportMessage>());

    /// <summary>
    /// Gets the number of messages in the queue with the given <paramref name="address"/>
    /// </summary>
    public int GetCount(string address) => _queues.TryGetValue(address, out var queue) ? queue.Count : 0;

    /// <summary>
    /// Gets the messages currently stored in the queue with the given <paramref name="address"/>
    /// </summary>
    public IReadOnlyList<InMemTransportMessage> GetMessages(string address) =>
        _queues.TryGetValue(address, out var queue)
            ? queue.ToArray()
            : Array.Empty<InMemTransportMessage>();

    /// <summary>
    /// Gets the subscribers for the current topic
    /// </summary>
    public IReadOnlyList<string> GetSubscribers(string topic)
    {
        if (topic == null) throw new ArgumentNullException(nameof(topic));

        return _subscribers.TryGetValue(topic, out var subscriberAddresses)
            ? subscriberAddresses.Keys.ToArray()
            : Array.Empty<string>();
    }

    /// <summary>
    /// Adds the subscriber with the given <paramref name="subscriberAddress"/> to the list of subscribers for the given <paramref name="topic"/>
    /// </summary>
    public void AddSubscriber(string topic, string subscriberAddress)
    {
        if (topic == null) throw new ArgumentNullException(nameof(topic));
        if (subscriberAddress == null) throw new ArgumentNullException(nameof(subscriberAddress));

        _subscribers.GetOrAdd(topic, _ => new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase))
            .TryAdd(subscriberAddress, null);
    }

    /// <summary>
    /// Removes the subscriber with the given <paramref name="subscriberAddress"/> from the list of subscribers for the given <paramref name="topic"/>
    /// </summary>
    public void RemoveSubscriber(string topic, string subscriberAddress)
    {
        if (subscriberAddress == null) throw new ArgumentNullException(nameof(subscriberAddress));

        if (!_subscribers.TryGetValue(topic, out var subscribers)) return;

        subscribers.TryRemove(subscriberAddress, out _);
    }

    static bool MessageIsExpired(InMemTransportMessage message)
    {
        if (!message.Headers.TryGetValue(Headers.TimeToBeReceived, out var timeToBeReceived)) return false;

        var maximumAge = TimeSpan.Parse(timeToBeReceived);

        return message.Age > maximumAge;
    }
}