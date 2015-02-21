using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus2.Messages;
using Rebus2.Messages.Control;
using Rebus2.Pipeline;
using Rebus2.Routing;
using Rebus2.Serialization;
using Rebus2.Transport;

namespace Rebus2.Persistence.InMem
{
    public class InMemorySubscriptionStorage : ISubscriptionStorage
    {
        static readonly StringComparer StringComparer = StringComparer.InvariantCultureIgnoreCase;
        readonly IRouter _router;
        readonly ITransport _transport;
        readonly IPipeline _pipeline;
        readonly IPipelineInvoker _pipelineInvoker;
        readonly ISerializer _serializer;

        readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _subscribers
            = new ConcurrentDictionary<string, ConcurrentDictionary<string, object>>(StringComparer);

        public InMemorySubscriptionStorage(IRouter router, ITransport transport, IPipeline pipeline, IPipelineInvoker pipelineInvoker, ISerializer serializer)
        {
            _router = router;
            _transport = transport;
            _pipeline = pipeline;
            _pipelineInvoker = pipelineInvoker;
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
                var headers = new Dictionary<string, string>();
                var logicalMessage = new Message(headers, new SubscribeRequest { SubscriberAddress = ownAddress });

                await _pipelineInvoker.Invoke(new StepContext(logicalMessage), _pipeline.SendPipeline());

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
            object dummy;

            _subscribers.GetOrAdd(topic, _ => new ConcurrentDictionary<string, object>(StringComparer))
                .TryRemove(topic, out dummy);
        }
    }
}