using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Extensions;
using Rebus.Subscriptions;
#pragma warning disable 1998

namespace Rebus.Persistence.FileSystem;

/// <summary>
/// Implementation of <see cref="ISubscriptionStorage"/> that stores subscriptions in a JSON file. Access to the file is synchronized within the process with a <see cref="ReaderWriterLockSlim"/>
/// </summary>
public class JsonFileSubscriptionStorage : ISubscriptionStorage, IDisposable
{
    static readonly Encoding FileEncoding = Encoding.UTF8;

    readonly ReaderWriterLockSlim _readerWriterLockSlim = new ReaderWriterLockSlim();
    readonly string _jsonFilePath;

    bool _disposed;

    /// <summary>
    /// Constructs the subscription storage
    /// </summary>
    public JsonFileSubscriptionStorage(string jsonFilePath, bool isCentralized = false)
    {
        _jsonFilePath = jsonFilePath ?? throw new ArgumentNullException(nameof(jsonFilePath));
        IsCentralized = isCentralized;
    }

    /// <summary>
    /// Gets all subscribers of the given topic from the JSON file
    /// </summary>
    public async Task<string[]> GetSubscriberAddresses(string topic)
    {
        // !!! DONT USE ASYNC/AWAIT IN HERE BECAUSE OF THE READERWRITERLOCKSLIM
        using (_readerWriterLockSlim.ReadLock())
        {
            var subscriptions = GetSubscriptions();

            return subscriptions.TryGetValue(topic, out var subscribers)
                ? subscribers.ToArray()
                : new string[0];
        }
    }

    /// <summary>
    /// Adds the subscriber to the list of subscribers from the given topic
    /// </summary>
    public async Task RegisterSubscriber(string topic, string subscriberAddress)
    {
        // !!! DONT USE ASYNC/AWAIT IN HERE BECAUSE OF THE READERWRITERLOCKSLIM
        using (_readerWriterLockSlim.WriteLock())
        {
            var subscriptions = GetSubscriptions();

            subscriptions
                .GetOrAdd(topic, () => new HashSet<string>())
                .Add(subscriberAddress);

            SaveSubscriptions(subscriptions);
        }
    }

    /// <summary>
    /// Removes the subscriber from the list of subscribers of the given topic
    /// </summary>
    public async Task UnregisterSubscriber(string topic, string subscriberAddress)
    {
        // !!! DONT USE ASYNC/AWAIT IN HERE BECAUSE OF THE READERWRITERLOCKSLIM
        using (_readerWriterLockSlim.WriteLock())
        {
            var subscriptions = GetSubscriptions();

            subscriptions
                .GetOrAdd(topic, () => new HashSet<string>())
                .Remove(subscriberAddress);

            SaveSubscriptions(subscriptions);
        }
    }

    void SaveSubscriptions(Dictionary<string, HashSet<string>> subscriptions)
    {
        // !!! DONT USE ASYNC/AWAIT IN HERE BECAUSE OF THE READERWRITERLOCKSLIM
        var jsonText = JsonConvert.SerializeObject(subscriptions, Formatting.Indented);

        File.WriteAllText(_jsonFilePath, jsonText, FileEncoding);
    }

    Dictionary<string, HashSet<string>> GetSubscriptions()
    {
        // !!! DONT USE ASYNC/AWAIT IN HERE BECAUSE OF THE READERWRITERLOCKSLIM
        try
        {
            var jsonText = File.ReadAllText(_jsonFilePath, FileEncoding);

            var subscriptions = JsonConvert.DeserializeObject<Dictionary<string, HashSet<string>>>(jsonText);

            return subscriptions;
        }
        catch (FileNotFoundException)
        {
            return new Dictionary<string, HashSet<string>>();
        }
    }

    /// <summary>
    /// Gets whether this subscription storage is centralized (which it shouldn't be - that would probably cause some pretty nasty locking exceptions!)
    /// </summary>
    public bool IsCentralized { get; }

    /// <summary>
    /// Disposes the <see cref="ReaderWriterLockSlim"/> that guards access to the file
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _readerWriterLockSlim.Dispose();
        }
        finally
        {
            _disposed = true;
        }
    }
}