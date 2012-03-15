using System;
using System.Reflection;
using Rebus.Logging;
using Rebus.Messages;

namespace Rebus.Bus
{
    /// <summary>
    /// Internal message handler, that handles subscription messages.
    /// </summary>
    class SubscriptionMessageHandler : IHandleMessages<SubscriptionMessage>
    {
        static ILog Log;

        static SubscriptionMessageHandler()
        {
            RebusLoggerFactory.Changed += f => Log = f.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        readonly IStoreSubscriptions storeSubscriptions;

        public SubscriptionMessageHandler(IStoreSubscriptions storeSubscriptions)
        {
            this.storeSubscriptions = storeSubscriptions;
        }

        public void Handle(SubscriptionMessage message)
        {
            var subscriberInputQueue = MessageContext.GetCurrent().ReturnAddress;
            var messageType = Type.GetType(message.Type);

            Log.Info("Saving: {0} subscribed to {1}", subscriberInputQueue, messageType);

            storeSubscriptions.Store(messageType, subscriberInputQueue);
        }
    }
}