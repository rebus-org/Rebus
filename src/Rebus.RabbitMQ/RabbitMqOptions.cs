using System;
using Rebus.Configuration;
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

        public RabbitMqOptions AddEventNameResolver(Func<Type, string> resolver)
        {
            queue.AddEventNameResolver(resolver);
            return this;
        }

        /// <summary>
        /// Configure the number of messages to prefetch/buffer per worker thread.
        /// </summary>
        /// <param name="prefetchCount">The number of messages to prefetch per worker thread. The default
        /// value is 100. A value of 0 will cause all available messages to be prefetched, which may lead
        /// to suboptimal performance or even crashes, if the queue size exceeds the maximum memory
        /// allowance for the endpoint application.</param>
        /// <returns>This <see cref="RabbitMqOptions"/> instance, allowing further configuration.</returns>
        public RabbitMqOptions PrefetchCount(ushort prefetchCount)
        {
            queue.PrefetchCount = prefetchCount;
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

            public void Store(Type eventType, string subscriberInputQueue)
            {
                rabbitMqMessageQueue.Subscribe(eventType, subscriberInputQueue);
            }

            public void Remove(Type eventType, string subscriberInputQueue)
            {
                rabbitMqMessageQueue.Unsubscribe(eventType, subscriberInputQueue);
            }

            public string[] GetSubscribers(Type eventType)
            {
                throw new InvalidOperationException("The RabbitMQ transport implementation is configured to let RabbitMQ" +
                                                    " manage subscriptions, so it is totaly unexpected that the GetSubscribers" +
                                                    " method got called!");
            }
        }
    }
}