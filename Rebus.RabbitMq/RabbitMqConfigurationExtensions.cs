using Rebus.Config;
using Rebus.Logging;
using Rebus.Subscriptions;
using Rebus.Transport;

namespace Rebus.RabbitMq
{
    /// <summary>
    /// Configuration extensions for the RabbitMQ transport
    /// </summary>
    public static class RabbitMqConfigurationExtensions
    {
        const string RabbitMqSubText = "The RabbitMQ transport was inserted as the subscriptions storage because it has native support for pub/sub messaging";

        /// <summary>
        /// Configures Rebus to use RabbitMQ to transport messages as a one-way client (i.e. will not be able to receive any messages)
        /// </summary>
        public static void UseRabbitMqAsOneWayClient(this StandardConfigurer<ITransport> configurer, string connectionString)
        {
            configurer
                .OtherService<RabbitMqTransport>()
                .Register(c =>
                {
                    var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                    return new RabbitMqTransport(connectionString, null, rebusLoggerFactory);
                });

            configurer
                .OtherService<ISubscriptionStorage>()
                .Register(c => c.Get<RabbitMqTransport>(), description: RabbitMqSubText);

            configurer.Register(c => c.Get<RabbitMqTransport>());

            OneWayClientBackdoor.ConfigureOneWayClient(configurer);
        }

        /// <summary>
        /// Configures Rebus to use RabbitMQ to move messages around
        /// </summary>
        public static RabbitMqOptionsBuilder UseRabbitMq(this StandardConfigurer<ITransport> configurer,  string connectionString, string inputQueueName)
        {
            var options = new RabbitMqOptionsBuilder();

            configurer
                .OtherService<RabbitMqTransport>()
                .Register(c =>
                {
                    var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                    var transport = new RabbitMqTransport(connectionString, inputQueueName, rebusLoggerFactory);

                    if (options.NumberOfMessagesToprefetch.HasValue)
                    {
                        transport.SetPrefetching(options.NumberOfMessagesToprefetch.Value);
                    }

                    return transport;
                });

            configurer
                .OtherService<ISubscriptionStorage>()
                .Register(c => c.Get<RabbitMqTransport>(), description: RabbitMqSubText);

            configurer.Register(c => c.Get<RabbitMqTransport>());

            return options;
        }
    }
}