using Rebus.Config;
using Rebus.Logging;
using Rebus.Transport;

namespace MsmqNonTransactionalTransport.Msmq
{
    /// <summary>
    /// Configuration extensions for the MSMQ transport
    /// </summary>
    public static class MsmqTransportConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use MSMQ to transport messages, receiving messages from the specified <paramref name="inputQueueName"/>
        /// </summary>
        public static void UseMsmq(this StandardConfigurer<ITransport> configurer, string inputQueueName)
        {
            configurer.Register(c =>
            {
                var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                return new MsmqNonTransactionalTransport(inputQueueName, rebusLoggerFactory);
            });
        }

        /// <summary>
        /// Configures Rebus to use MSMQ to transport messages as a one-way client (i.e. will not be able to receive any messages)
        /// </summary>
        public static void UseMsmqAsOneWayClient(this StandardConfigurer<ITransport> configurer)
        {
            configurer.Register(c =>
            {
                var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                return new MsmqNonTransactionalTransport(null, rebusLoggerFactory);
            });

            OneWayClientBackdoor.ConfigureOneWayClient(configurer);
        }

        /// <summary>
        /// Configures Rebus to use MSMQ to transport messages using a non transactional queue.
        /// </summary>
        /// <param name="configurer"></param>
        /// <param name="inputQueueName"></param>
        public static void UseMsmqAsNonTransactional(this StandardConfigurer<ITransport> configurer, string inputQueueName)
        {
            configurer.Register(c =>
            {
                var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                return new MsmqNonTransactionalTransport(inputQueueName, rebusLoggerFactory);
            });
        }

        /// <summary>
        /// Configures Rebus to use MSMQ to transport messages as a one-way client (i.e. will not be able to receive any messages) using a non transactional queue.
        /// </summary>
        /// <param name="configurer"></param>
        public static void UseMsmqAsOneWayClientAndNonTransactional(this StandardConfigurer<ITransport> configurer)
        {
            configurer.Register(c =>
            {
                var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                return new MsmqNonTransactionalTransport(null, rebusLoggerFactory);
            });

            OneWayClientBackdoor.ConfigureOneWayClient(configurer);
        }
    }
}