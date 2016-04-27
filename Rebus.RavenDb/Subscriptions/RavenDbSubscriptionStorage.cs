using System;
using System.Linq;
using Raven.Client;
using Rebus.Subscriptions;
using System.Threading.Tasks;
using Raven.Abstractions.Exceptions;
using Rebus.Logging;

namespace Rebus.RavenDb.Subscriptions
{
    /// <summary>
    /// Implementation of <see cref="ISubscriptionStorage"/> that stores subscriptions in RavenDB
    /// </summary>
    public class RavenDbSubscriptionStorage : ISubscriptionStorage
    {
        readonly IDocumentStore _documentStore;
        readonly ILog _logger;

        /// <summary>
        /// Constructs the subscription storage using the specified document store. Can be configured to be centralized
        /// </summary>
        public RavenDbSubscriptionStorage(IDocumentStore documentStore, bool isCentralized, IRebusLoggerFactory rebusLoggerFactory)
        {
            if (documentStore == null) throw new ArgumentNullException(nameof(documentStore));
            if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
            _documentStore = documentStore;
            IsCentralized = isCentralized;
            _logger = rebusLoggerFactory.GetCurrentClassLogger();
        }

        /// <summary>
        /// Gets whether this particular subscription storage is centralized
        /// </summary>
        public bool IsCentralized { get; }

        /// <summary>
        /// Gets all destination addresses for the given topic
        /// </summary>
        public async Task<string[]> GetSubscriberAddresses(string topic)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                var topicDocument = await session.LoadAsync<Topic>(topic);

                return topicDocument?.SubscriberAddresses.ToArray()
                    ?? new string[0];
            }
        }

        /// <summary>
        /// Registers the given <paramref name="subscriberAddress"/> as a subscriber of the given topic
        /// </summary>
        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            var attempt = 0;
            while (true)
            {
                attempt++;
                try
                {
                    await InnerRegisterSubscriber(topic, subscriberAddress);
                    return;
                }
                catch (ConcurrencyException exception)
                {
                    if (attempt >= 100)
                    {
                        throw new ConcurrencyException($"Could not complete subscriber registration, even after {attempt} attempts!", exception);
                    }

                    if (attempt % 10 == 0)
                    {
                        _logger.Warn("Did not successfully register subscriber '{0}' in topic document '{1}', even after {2} attempts",
                            subscriberAddress, topic, attempt);
                    }
                }
            }
        }

        /// <summary>
        /// Unregisters the given <paramref name="subscriberAddress"/> as a subscriber of the given topic
        /// </summary>
        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            var attempt = 0;
            while (true)
            {
                attempt++;
                try
                {
                    await InnerUnregisterSubscriber(topic, subscriberAddress);
                    return;
                }
                catch (ConcurrencyException exception)
                {
                    if (attempt >= 100)
                    {
                        throw new ConcurrencyException($"Could not complete subscriber unregistration, even after {attempt} attempts!", exception);
                    }

                    if (attempt % 10 == 0)
                    {
                        _logger.Warn("Did not successfully unregister subscriber '{0}' in topic document '{1}', even after {2} attempts",
                            subscriberAddress, topic, attempt);
                    }
                }
            }
        }

        async Task InnerUnregisterSubscriber(string topic, string subscriberAddress)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;

                var topicDocument = await session.LoadAsync<Topic>(topic);

                if (topicDocument == null) return;

                topicDocument.Unregister(subscriberAddress);

                await session.SaveChangesAsync();
            }
        }

        async Task InnerRegisterSubscriber(string topic, string subscriberAddress)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;

                var topicDocument = await session.LoadAsync<Topic>(topic);

                if (topicDocument == null)
                {
                    topicDocument = new Topic(topic, Enumerable.Empty<string>());
                    await session.StoreAsync(topicDocument);
                }

                if (topicDocument.HasSubscriber(subscriberAddress)) return;

                topicDocument.Register(subscriberAddress);

                await session.SaveChangesAsync();
            }
        }
    }
}