using System;
using Rebus.Configuration;
using Rebus.Messages;

namespace Rebus.RabbitMQ
{
    /// <summary>
    /// Fluent options setter that allows for configuring various aspects of the RabbitMQ transport
    /// </summary>
    public class RabbitMqOptions
    {
        readonly RabbitMqMessageQueue queue;
        readonly RebusTransportConfigurer configurer;
        bool createErrorQueue = true;

        internal RabbitMqOptions(RabbitMqMessageQueue queue, RebusTransportConfigurer configurer)
        {
            this.queue = queue;
            this.configurer = configurer;
        }

        /// <summary>
        /// Indicates whether the Rabbit options dictate that an error queue should be created
        /// </summary>
        public bool CreateErrorQueue
        {
            get { return createErrorQueue; }
        }

        /// <summary>
        /// Configures whether or not outgoing messages of this type (including derived types) should have
        /// their 'persistent' flag set in the message properties.
        /// </summary>
        public RabbitMqOptions ConfigurePersistenceFor<TMessage>(bool persistent)
        {
            configurer
                .Backbone
                .ConfigureEvents(e =>
                    {
                        e.MessageSent += (bus, destination, message) =>
                            {
                                if (!(message is TMessage)) return;

                                bus.AttachHeader(message,
                                                 RabbitMqMessageQueue.InternalHeaders.MessageDurability,
                                                 persistent.ToString());
                            };
                    });
            return this;
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
        /// Uses the exchange per type routing, instead of Rebus' default behaviour of using a single topic-based exchange.
        /// This allows for a little more performance (fanout vs topic) and also allows forwarding of messages to other
        /// rabbitmq servers by manually setting federated queues/exchanges.
        /// </summary>
        /// <returns></returns>
        public RabbitMqOptions UseOneExchangePerMessageTypeRouting()
        {
            queue.UseExchange(null);
            return this;
        }

        /// <summary>
        /// Uses the exchange as input address, by binding it to subscribed type's exchange(s).
        /// This setting only works in conjuntion with 'One Exchange Per Message Type' routing.
        /// </summary>
        /// <param name="exchangeName">Name of the exchange.</param>
        /// <returns></returns>
        public RabbitMqOptions UseExchangeAsInputAddress(string exchangeName)
        {
            queue.UseExchangeAsInputAddress(exchangeName);
            return this;
        }

        /// <summary>
        /// Disables Rebus' default behavior of (re)declaring the input exhange when it first interacts with Rabbit.
        /// This setting only works in conjuntion with 'One Exchange Per Message Type' routing.
        /// </summary>
        /// <returns></returns>
        public RabbitMqOptions DoNotDeclareInputExchange()
        {
            queue.DoNotDeclareInputExchange();
            return this;
        }

        /// <summary>
        /// Disables automatic creation of the error queue. This means that the Rebus error queue setting merely becomes the
        /// topic under which failed messages will be published, thus allowing you to route failed messages wherever you want.
        /// WARNING: It also means that failed messages ARE LOST if no queue exists that is bound to the topic.
        /// </summary>
        public RabbitMqOptions DoNotCreateErrorQueue()
        {
            createErrorQueue = false;
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
        /// Immediately deletes all messages currently in the queue.
        /// </summary>
        public RabbitMqOptions PurgeInputQueue()
        {
            queue.PurgeInputQueue();
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