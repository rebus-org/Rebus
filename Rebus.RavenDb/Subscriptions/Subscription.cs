using System.Collections.Generic;
using System.Linq;

namespace Rebus.RavenDb.Subscriptions
{
    /// <summary>
    /// RavenDb document to contain subscription informations
    /// </summary>
    public class Subscription
    {
        public const string Id = "Subscriptions/1";
        public Dictionary<string, HashSet<string>> Subscriptions { get; private set; }

        public Subscription()
        {
            Subscriptions = new Dictionary<string, HashSet<string>>();
        }

        public void RegisterSubscriber(string topic, string subscriberAddress)
        {
            if (Subscriptions.ContainsKey(topic))
            {
                Subscriptions[topic].Add(subscriberAddress);
                return;
            }

            Subscriptions.Add(topic, new HashSet<string> { subscriberAddress });

        }

        public void UnregisterSubscriber(string topic, string subscriberAddress)
        {
            if (Subscriptions.ContainsKey(topic))
            {
                Subscriptions[topic].Remove(subscriberAddress);
                if (Subscriptions[topic].Any() == false)
                {
                    Subscriptions.Remove(topic);
                }
            }
        }

        public string[] GetSubscriberAddresses(string topic)
        {
            if (Subscriptions.ContainsKey(topic))
            {
                return Subscriptions[topic].ToArray();
            }

            return new string[0];
        }

    }
}