using System;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Messages;

namespace Rebus.RabbitMQ
{
    public class RabbitMqOptions
    {
        readonly RabbitMqMessageQueue queue;
        readonly RebusTransportConfigurer configurer;

        public RabbitMqOptions(RabbitMqMessageQueue queue, RebusTransportConfigurer configurer)
        {
            this.queue = queue;
            this.configurer = configurer;
        }

        public RabbitMqOptions ManageSubscriptions()
        {
            queue.ManageSubscriptions();
            configurer.AddDecoration(b =>
                {
                    // ensure that subscription storage is not used!
                    b.StoreSubscriptions = new RabbitMqSubscriptionStorageRelay(queue);
                });
            return this;
        }

        /// <summary>
        /// The RabbitMQ subscriptions storage handles the event where a subscriber is not configured to let
        /// RabbitMQ manage subscriptions. In this case, a <see cref="SubscriptionMessage"/> will be sent to
        /// the publisher, so we just subscribe on the subscriber's behalf.
        /// </summary>
        class RabbitMqSubscriptionStorageRelay : IStoreSubscriptions
        {
            readonly RabbitMqMessageQueue rabbitMqMessageQueue;

            public RabbitMqSubscriptionStorageRelay(RabbitMqMessageQueue rabbitMqMessageQueue)
            {
                if (!rabbitMqMessageQueue.ManagesSubscriptions)
                {
                    throw new InvalidOperationException("The RabbitMqSubscriptionStorageRelay should only be used when the Rabbit transport is configured to let Rabbit manage subscriptions");
                }
                this.rabbitMqMessageQueue = rabbitMqMessageQueue;
            }

            public void Store(Type messageType, string subscriberInputQueue)
            {
                rabbitMqMessageQueue.Subscribe(messageType, subscriberInputQueue);
            }

            public void Remove(Type messageType, string subscriberInputQueue)
            {
                rabbitMqMessageQueue.Unsubscribe(messageType, subscriberInputQueue);
            }

            public string[] GetSubscribers(Type messageType)
            {
                throw new InvalidOperationException("The RabbitMQ transport implementation is configured to let RabbitMQ" +
                                                    " manage subscriptions, so it is totaly unexpected that the GetSubscribers" +
                                                    " method got called!");
            }
        }
    }
}