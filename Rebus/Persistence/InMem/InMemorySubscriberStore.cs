using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Persistence.InMem;

/// <summary>
/// In-mem subscriber store that can be shared among multiple endpoints to emulate a shared subscription storage
/// </summary>
public class InMemorySubscriberStore
{
    static readonly StringComparer StringComparer = StringComparer.OrdinalIgnoreCase;
    static readonly string[] NoSubscribers = Array.Empty<string>();

    readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _subscribers = new(StringComparer);

    /// <summary>
    /// Gets all topics which are known by the subscriber store. 
    /// </summary>
    public IEnumerable<string> Topics => _subscribers.Keys.ToList();

    /// <summary>
    /// Gets the subscribers for the current topic
    /// </summary>
    public string[] GetSubscribers(string topic)
    {
        if (topic == null) throw new ArgumentNullException(nameof(topic));
        
        return _subscribers.TryGetValue(topic, out var subscriberAddresses)
            ? subscriberAddresses.Keys.ToArray()
            : NoSubscribers;
    }

    /// <summary>
    /// Adds the subscriber with the given <paramref name="subscriberAddress"/> to the list of subscribers for the given <paramref name="topic"/>
    /// </summary>
    public void AddSubscriber(string topic, string subscriberAddress)
    {
        if (topic == null) throw new ArgumentNullException(nameof(topic));
        if (subscriberAddress == null) throw new ArgumentNullException(nameof(subscriberAddress));

        _subscribers.GetOrAdd(topic, _ => new ConcurrentDictionary<string, object>(StringComparer))
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

    /// <summary>
    /// Resets the subscriber store (i.e. all known topics and their subscriptions are deleted)
    /// </summary>
    public void Reset() => _subscribers.Clear();
}