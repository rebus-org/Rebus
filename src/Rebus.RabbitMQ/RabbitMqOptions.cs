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

        /// <summary>
        /// Specifies that Rabbit should be used to route messages when doing multicast. This effectively
        /// lets Rabbit be the subscription storage, and allows for subscribing to messages without having
        /// any endpoint mappings at all.
        /// </summary>
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
        /// Configure the exchange through which messages are routed to endpoint queues.
        /// </summary>
        /// <param name="exchangeName">The name of the RabbitMQ exchange.</param>
        public RabbitMqOptions UseExchange(string exchangeName)
        {
            queue.UseExchange(exchangeName);
            return this;
        }

        /// <summary>
        /// Disables Rebus' default behavior of (re)declaring the exhange when it first interacts with Rabbit.
        /// </summary>
        public RabbitMqOptions DoNotDeclareExchange()
        {
            queue.DoNotDeclareExchange();
            return this;
        }

        /// <summary>
        /// Disables Rebus' default behavior of creating a binding to the service's input queue from a topic with the same name.
        /// If this is disabled, the service can not be addressed directly by other services, and the service cannot send
        /// a message to itself with <see cref="IBus.SendLocal{TCommand}"/>
        /// </summary>
        public RabbitMqOptions DoNotBindDefaultTopicToInputQueue()
        {
            queue.DoNotBindDefaultTopicToInputQueue();
            return this;
        }

        /// <summary>
        /// Specifies that the input queue will be created with the auto-delete flag set, which results
        /// in immediate deletion when we disconnect.
        /// </summary>
        public RabbitMqOptions AutoDeleteInputQueue()
        {
            queue.AutoDeleteInputQueue();
            return this;
        }

        /// <summary>
        /// Adds an event name resolver to the list of resolvers that are used each time Rebus must find out
        /// which topic to publish to, given the type of the published event. You can add multiple resolvers,
        /// and they will be called in the order they were added until a not-null result is returned, ultimately
        /// falling back to the default behavior of using a prettified full name of the .NET type as the
        /// topic. E.g. .NET strings will be published to "System.String", a generic list of time spans will be published
        /// to "System.Collections.Generic.List&lt;System.TimeSpan&gt;" etc.
        /// </summary>
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
        public RabbitMqOptions SetPrefetchCount(ushort prefetchCount)
        {
            queue.Prefetch(prefetchCount);
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