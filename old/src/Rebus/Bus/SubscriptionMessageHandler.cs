using System;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Shared;

namespace Rebus.Bus
{
    /// <summary>
    /// Internal message handler that handles subscription messages.
    /// </summary>
    class SubscriptionMessageHandler : IHandleMessages<SubscriptionMessage>
    {
        static ILog log;

        static SubscriptionMessageHandler()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly IStoreSubscriptions storeSubscriptions;

        public SubscriptionMessageHandler(IStoreSubscriptions storeSubscriptions)
        {
            this.storeSubscriptions = storeSubscriptions;
        }

        public void Handle(SubscriptionMessage message)
        {
            var messageContext = MessageContext.GetCurrent();
            var subscriberInputQueue = messageContext.ReturnAddress;

            if (string.IsNullOrWhiteSpace(subscriberInputQueue))
            {
                throw new ArgumentOutOfRangeException(
                    string.Format(
                        "Invalid subscription message for {0}/{1} - could not find subscriber's input queue! Transport message must contain a proper {2} in order to be able to subscribe to messages",
                        message.Type, message.Action,
                        Headers.ReturnAddress));
            }

            Type messageType;
            try
            {
                messageType = Type.GetType(message.Type);
            }
            catch(Exception e)
            {
                throw new ArgumentOutOfRangeException(
                    string.Format("Invalid subscription message received for messages of type '{0}' (from {1}, attempted to {2}) - an exception occurred while attempting to deduce the .NET type",
                                  message.Type, subscriberInputQueue, message.Action), e);
            }

            switch (message.Action)
            {
                case SubscribeAction.Subscribe:
                    log.Info("Saving: {0} subscribed to {1}", subscriberInputQueue, messageType);
                    storeSubscriptions.Store(messageType, subscriberInputQueue);
                    break;
                case SubscribeAction.Unsubscribe:
                    log.Info("Saving: {0} unsubscribe to {1}", subscriberInputQueue, messageType);
                    storeSubscriptions.Remove(messageType, subscriberInputQueue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        string.Format("Unknown subscribe action {0} on subscription message from {1}",
                                      message.Action, subscriberInputQueue));
            }
        }
    }
}