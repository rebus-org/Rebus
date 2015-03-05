using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Messages.Control;
using Rebus.Routing;

namespace Rebus.Handlers
{
    public class InternalHandlersContributor : IHandlerActivator
    {
        readonly IHandlerActivator _innerHandlerActivator;
        readonly ISubscriptionStorage _subscriptionStorage;
        readonly Dictionary<Type, IEnumerable> _internalHandlers;

        public InternalHandlersContributor(IHandlerActivator innerHandlerActivator, ISubscriptionStorage subscriptionStorage)
        {
            _innerHandlerActivator = innerHandlerActivator;
            _subscriptionStorage = subscriptionStorage;

            _internalHandlers = new Dictionary<Type, IEnumerable>
            {
                {typeof (SubscribeRequest), new object[] {new SubscribeRequestHandler(subscriptionStorage)}},
                {typeof (UnsubscribeRequest), new object[] {new UnsubscribeRequestHandler(subscriptionStorage)}}
            };
        }

        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message)
        {
            var ownHandlers = GetOwnHandlersFor<TMessage>();

            var handlers = await _innerHandlerActivator.GetHandlers(message);

            return handlers.Concat(ownHandlers);
        }

        IEnumerable<IHandleMessages<TMessage>> GetOwnHandlersFor<TMessage>()
        {
            IEnumerable ownHandlers;

            return _internalHandlers.TryGetValue(typeof (TMessage), out ownHandlers)
                ? ownHandlers.OfType<IHandleMessages<TMessage>>()
                : Enumerable.Empty<IHandleMessages<TMessage>>();
        }

        class SubscribeRequestHandler : IHandleMessages<SubscribeRequest>
        {
            readonly ISubscriptionStorage _subscriptionStorage;

            public SubscribeRequestHandler(ISubscriptionStorage subscriptionStorage)
            {
                _subscriptionStorage = subscriptionStorage;
            }

            public async Task Handle(SubscribeRequest message)
            {
                await _subscriptionStorage.RegisterSubscriber(message.Topic, message.SubscriberAddress);
            }
        }

        class UnsubscribeRequestHandler : IHandleMessages<UnsubscribeRequest>
        {
            readonly ISubscriptionStorage _subscriptionStorage;

            public UnsubscribeRequestHandler(ISubscriptionStorage subscriptionStorage)
            {
                _subscriptionStorage = subscriptionStorage;
            }

            public async Task Handle(UnsubscribeRequest message)
            {
                await _subscriptionStorage.UnregisterSubscriber(message.Topic, message.SubscriberAddress);
            }
        }
    }
}