using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Messages.Control;
using Rebus.Routing;
using Rebus.Serialization;
using Rebus.Subscriptions;
using Rebus.Transport;

namespace Rebus.Persistence.InMem
{
    public class InMemorySubscriptionStorage : ISubscriptionStorage
    {
        static readonly StringComparer StringComparer = StringComparer.InvariantCultureIgnoreCase;

        readonly IRouter _router;
        readonly ITransport _transport;
        readonly ISerializer _serializer;

        readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _subscribers
            = new ConcurrentDictionary<string, ConcurrentDictionary<string, object>>(StringComparer);

        public InMemorySubscriptionStorage(IRouter router, ITransport transport, ISerializer serializer)
        {
            _router = router;
            _transport = transport;
            _serializer = serializer;
        }

        public async Task<IEnumerable<string>> GetSubscriberAddresses(string topic)
        {
            ConcurrentDictionary<string, object> subscriberAddresses;

            return _subscribers.TryGetValue(topic, out subscriberAddresses)
                ? subscriberAddresses.Keys
                : Enumerable.Empty<string>();
        }

        public async Task RegisterSubscriber(string topic, string subscriberAddress)
        {
            var ownerAddress = await _router.GetOwnerAddress(topic);
            var ownAddress = _transport.Address;

            // see if it's necessary to send a request to someone else
            if (ownerAddress == ownAddress)
            {
                _subscribers.GetOrAdd(topic, _ => new ConcurrentDictionary<string, object>(StringComparer))
                    .TryAdd(subscriberAddress, new object());
            }
            else
            {
                var headers = new Dictionary<string, string>
                {
                    {Headers.MessageId, Guid.NewGuid().ToString()}
                };

                var logicalMessage = new Message(headers, new SubscribeRequest
                {
                    SubscriberAddress = ownAddress,
                    Topic = topic
                });

                var transportMessage = await _serializer.Serialize(logicalMessage);

                var transactionContext = AmbientTransactionContext.Current;

                if (transactionContext == null)
                {
                    using (var defaultTransactionContext = new DefaultTransactionContext())
                    {
                        await _transport.Send(ownerAddress, transportMessage, defaultTransactionContext);

                        defaultTransactionContext.Complete();
                    }
                }
                else
                {
                    await _transport.Send(ownerAddress, transportMessage, transactionContext);
                }
            }
        }

        public async Task UnregisterSubscriber(string topic, string subscriberAddress)
        {
            var ownerAddress = await _router.GetOwnerAddress(topic);
            var ownAddress = _transport.Address;

            // see if it's necessary to send a request to someone else
            if (ownerAddress == ownAddress)
            {
                object dummy;

                _subscribers.GetOrAdd(topic, _ => new ConcurrentDictionary<string, object>(StringComparer))
                    .TryRemove(subscriberAddress, out dummy);
            }
            else
            {
                var headers = new Dictionary<string, string>
                {
                    {Headers.MessageId, Guid.NewGuid().ToString()}
                };

                var logicalMessage = new Message(headers, new UnsubscribeRequest
                {
                    SubscriberAddress = ownAddress,
                    Topic = topic
                });

                var transportMessage = await _serializer.Serialize(logicalMessage);

                var transactionContext = AmbientTransactionContext.Current;

                if (transactionContext == null)
                {
                    using (var defaultTransactionContext = new DefaultTransactionContext())
                    {
                        await _transport.Send(ownerAddress, transportMessage, defaultTransactionContext);

                        defaultTransactionContext.Complete();
                    }
                }
                else
                {
                    await _transport.Send(ownerAddress, transportMessage, transactionContext);
                }
            }
        }

        public bool IsCentralized
        {
            get
            {
                // in-mem subscription storage is decentralized
                return false;
            }
        }
    }
}