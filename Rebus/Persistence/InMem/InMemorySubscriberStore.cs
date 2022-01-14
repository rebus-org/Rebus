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
    static readonly string[] NoSubscribers = new string[0];

    readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _subscribers = new ConcurrentDictionary<string, ConcurrentDictionary<string, object>>(StringComparer);

    /// <summary>
    /// Gets all topics which are known by the subscriber store. 
    /// </summary>
    public IEnumerable<string> Topics => _subscribers.Keys.ToList();

    /// <summary>
    /// Gets the subscribers for the current topic
    /// </summary>
    public string[] GetSubscribers(string topic)
    {
        ConcurrentDictionary<string, object> subscriberAddresses;

        return _subscribers.TryGetValue(topic, out subscriberAddresses)
            ? subscriberAddresses.Keys.ToArray()
            : NoSubscribers;

    }

    /// <summary>
    /// Adds the subscriber with the given <paramref name="subscriberAddress"/> to the list of subscribers for the given <paramref name="topic"/>
    /// </summary>
    public void AddSubscriber(string topic, string subscriberAddress)
    {
        _subscribers.GetOrAdd(topic, _ => new ConcurrentDictionary<string, object>(StringComparer))
            .TryAdd(subscriberAddress, new object());
    }

    /// <summary>
    /// Removes the subscriber with the given <paramref name="subscriberAddress"/> from the list of subscribers for the given <paramref name="topic"/>
    /// </summary>
    public void RemoveSubscriber(string topic, string subscriberAddress)
    {
        object dummy;

        _subscribers.GetOrAdd(topic, _ => new ConcurrentDictionary<string, object>(StringComparer))
            .TryRemove(subscriberAddress, out dummy);
    }

    /// <summary>
    /// Resets the subscriber store (i.e. all known topics and their subscriptions are deleted)
    /// </summary>
    public void Reset()
    {
        _subscribers.Clear();
    }
}