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
    }
}