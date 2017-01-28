using Rebus.Config;

namespace Rebus.Transport.InMem
{
    /// <summary>
    /// Configuration extensions for the in-mem transport
    /// </summary>
    public static class InMemTransportConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use in-mem message queues, delivering/receiving from the specified <see cref="InMemNetwork"/>
        /// </summary>
        public static void UseInMemoryTransport(this StandardConfigurer<ITransport> configurer, InMemNetwork network, string inputQueueName)
        {
            configurer.Register(context => new InMemTransport(network, inputQueueName));
        }

        /// <summary>
        /// Configures Rebus to use in-mem message queues, configuring this instance to be a one-way client
        /// </summary>
        public static void UseInMemoryTransportAsOneWayClient(this StandardConfigurer<ITransport> configurer, InMemNetwork network)
        {
            configurer.Register(c => new InMemTransport(network, null));

            OneWayClientBackdoor.ConfigureOneWayClient(configurer);
        }
    }
}