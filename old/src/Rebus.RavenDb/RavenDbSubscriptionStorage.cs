using System;
using System.Collections.Generic;
using Raven.Client;

namespace Rebus.RavenDb
{
    public class RavenDbSubscriptionStorage : IStoreSubscriptions
    {
        readonly IDocumentStore store;
        readonly string collection;

        public RavenDbSubscriptionStorage(IDocumentStore store, string collection)
        {
            this.store = store;
            this.collection = collection;
        }

        public void Store(Type eventType, string subscriberInputQueue)
        {
            using (var session = store.OpenSession())
            {
                var subscription = EnsureSubscription(session, eventType);
                if (!subscription.Endpoints.Contains(subscriberInputQueue))
                    subscription.Endpoints.Add(subscriberInputQueue);
                session.SaveChanges();
            }
        }

        public void Remove(Type eventType, string subscriberInputQueue)
        {
            using (var session = store.OpenSession())
            {
                var subscription = EnsureSubscription(session, eventType);
                subscription.Endpoints.Remove(subscriberInputQueue);
                session.SaveChanges();
            }
        }

        public string[] GetSubscribers(Type eventType)
        {
            using (var session = store.OpenSession())
            {
                var subscription = session.Load<RebusSubscription>(Key(eventType));
                return subscription == null ? new string[0] : subscription.Endpoints.ToArray();
            }
        }

        RebusSubscription EnsureSubscription(IDocumentSession session, Type messageType)
        {
            var subscription = session.Load<RebusSubscription>(Key(messageType));
            if (subscription == null)
            {
                var newSubscription = new RebusSubscription
                {
                    Id = Key(messageType)
                };
                session.Store(newSubscription);
                return newSubscription;
            }

            return subscription;
        }

        string Key(Type messageType)
        {
            return string.Format("{0}/{1}", collection, messageType.FullName);
        }

        class RebusSubscription
        {
            public RebusSubscription()
            {
                Endpoints = new List<string>();
            }

            public string Id { get; set; }
            public List<string> Endpoints { get; set; }
        }
    }
}