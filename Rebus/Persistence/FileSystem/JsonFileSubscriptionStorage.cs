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

namespace Rebus.Persistence.FileSystem
{
    /// <summary>
    /// Implementation of <see cref="ISubscriptionStorage"/> that stores subscriptions in a JSON file
    /// </summary>
    public class JsonFileSubscriptionStorage : ISubscriptionStorage
    {
        static readonly Encoding FileEncoding = Encoding.UTF8;

        readonly ReaderWriterLockSlim _readerWriterLockSlim = new ReaderWriterLockSlim();
        readonly string _jsonFilePath;

        /// <summary>
        /// Constructs the subscription storage
        /// </summary>
        public JsonFileSubscriptionStorage(string jsonFilePath)
        {
            _jsonFilePath = jsonFilePath;
        }

        /// <summary>
        /// Gets all subscribers of the given topic from the JSON file
        /// </summary>
        public async Task<string[]> GetSubscriberAddresses(string topic)
        {
            try
            {
                _readerWriterLockSlim.EnterReadLock();

                var subscriptions = GetSubscriptions();

                HashSet<string> subscribers;

                return subscriptions.TryGetValue(topic, out subscribers)
                    ? subscribers.ToArray()
                    : new string[0];
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
        }

        /// <summary>
        /// Adds the subscriber to the list of subscribers from the given topic
        /// </summary>
        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            try
            {
                _readerWriterLockSlim.EnterWriteLock();

                var subscriptions = GetSubscriptions();

                subscriptions
                    .GetOrAdd(topic, () => new HashSet<string>())
                    .Add(subscriberAddress);

                SaveSubscriptions(subscriptions);
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes the subscriber from the list of subscribers of the given topic
        /// </summary>
        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            try
            {
                _readerWriterLockSlim.EnterWriteLock();

                var subscriptions = GetSubscriptions();

                subscriptions
                    .GetOrAdd(topic, () => new HashSet<string>())
                    .Remove(subscriberAddress);

                SaveSubscriptions(subscriptions);
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }
        }

        void SaveSubscriptions(Dictionary<string, HashSet<string>> subscriptions)
        {
            var jsonText = JsonConvert.SerializeObject(subscriptions, Formatting.Indented);

            File.WriteAllText(_jsonFilePath, jsonText, FileEncoding);
        }

        Dictionary<string, HashSet<String>> GetSubscriptions()
        {
            try
            {
                var jsonText = File.ReadAllText(_jsonFilePath, FileEncoding);

                var subscriptions = JsonConvert.DeserializeObject<Dictionary<string, HashSet<String>>>(jsonText);

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
        public bool IsCentralized
        {
            get { return false; }
        }
    }
}