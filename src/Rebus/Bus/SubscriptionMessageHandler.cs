using System;
using System.Reflection;
using Rebus.Messages;
using log4net;

namespace Rebus.Bus
{
    /// <summary>
    /// Internal message handler, that handles subscription messages.
    /// </summary>
    class SubscriptionMessageHandler : IHandleMessages<SubscriptionMessage>
    {
        static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        readonly IStoreSubscriptions storeSubscriptions;

        public SubscriptionMessageHandler(IStoreSubscriptions storeSubscriptions)
        {
            this.storeSubscriptions = storeSubscriptions;
        }

        public void Handle(SubscriptionMessage message)
        {
            var subscriberInputQueue = MessageContext.GetCurrent().ReturnAddressOfCurrentTransportMessage;
            var messageType = Type.GetType(message.Type);

            Log.InfoFormat("Saving: {0} subscribed to {1}", subscriberInputQueue, messageType);

            storeSubscriptions.Save(messageType, subscriberInputQueue);
        }
    }
}