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

        public bool IsCentralized
        {
            get;
            private set;
        }

        //class Subscriptions
        //{
        //    public Subscriptions()
        //    {
        //        Topics = new List<TopicSubs>();
        //    }
            
        //    public List<TopicSubs> Topics { get; private set; }

        //    public string[] ForTopic(string topic)
        //    {
        //        var topicSub = Topics.FirstOrDefault(t => t.Matches(topic));
        //        if (topicSub == null) return new string[0];

        //        return topicSub.Subscribers.ToArray();
        //    }

        //    public void Subscribe(string topic, string subscriberAddress)
        //    {
        //        var topicSub = Topics.FirstOrDefault(t => t.Matches(topic));
        //        if (topicSub == null)
        //        {
        //            topicSub = new TopicSubs(topic);
        //            Topics.Add(topicSub);
        //        }
        //        topicSub.Subscribers.Add(subscriberAddress);
        //    }

        //    public void Unsubscribe(string topic, string subscriberAddress)
        //    {
        //        var topicSub = Topics.FirstOrDefault(t => t.Matches(topic));
        //        if (topicSub == null) return;
                
        //        topicSub.Subscribers.Remove(subscriberAddress);
        //    }
        //}

        class TopicSubs
        {
            public TopicSubs(string topic)
            {
                if (topic == null) throw new ArgumentNullException("topic");
                Topic = topic;
                Subscribers = new HashSet<string>();
            }

            public string Topic { get; private set; }
            
            public HashSet<string> Subscribers { get; private set; }

            public bool Matches(string topic)
            {
                return topic == Topic;
            }
        }
    }
}