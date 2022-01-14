using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Messages.Control;
using Rebus.Subscriptions;
using Rebus.Transport;

namespace Rebus.Handlers;

/// <summary>
/// Decoration of <see cref="IHandlerActivator"/> that adds a few special handlers when an incoming message can be recognized
/// as a special Rebus message
/// </summary>
class InternalHandlersContributor : IHandlerActivator
{
    readonly IHandlerActivator _innerHandlerActivator;
    readonly Dictionary<Type, IHandleMessages[]> _internalHandlers;

    public InternalHandlersContributor(IHandlerActivator innerHandlerActivator, ISubscriptionStorage subscriptionStorage)
    {
        _innerHandlerActivator = innerHandlerActivator;

        _internalHandlers = new Dictionary<Type, IHandleMessages[]>
        {
            {typeof (SubscribeRequest), new IHandleMessages[] {new SubscribeRequestHandler(subscriptionStorage)}},
            {typeof (UnsubscribeRequest), new IHandleMessages[] {new UnsubscribeRequestHandler(subscriptionStorage)}}
        };
    }

    /// <summary>
    /// Gets Rebus' own internal handlers (if any) for the given message type
    /// </summary>
    public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
    {
        var ownHandlers = GetOwnHandlersFor<TMessage>();

        var handlers = await _innerHandlerActivator.GetHandlers(message, transactionContext);

        return handlers.Concat(ownHandlers);
    }

    IEnumerable<IHandleMessages<TMessage>> GetOwnHandlersFor<TMessage>()
    {
        return _internalHandlers.TryGetValue(typeof(TMessage), out var ownHandlers)
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