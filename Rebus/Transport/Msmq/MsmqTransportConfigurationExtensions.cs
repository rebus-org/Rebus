using Rebus.Config;

namespace Rebus.Transport.Msmq
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
            configurer.Register(context => new MsmqTransport(inputQueueName));
        }

        /// <summary>
        /// Configures Rebus to use MSMQ to transport messages as a one-way client (i.e. will not be able to receive any messages)
        /// </summary>
        public static void UseMsmqAsOneWayClient(this StandardConfigurer<ITransport> configurer)
        {
            configurer.Register(context => new MsmqTransport(null));

            OneWayClientBackdoor.ConfigureOneWayClient(configurer);
        }
    }
}