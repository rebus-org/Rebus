using Rebus2.Config;

namespace Rebus2.Transport.InMem
{
    public static class InMemTransportConfigurationExtensions
    {
        public static void UseInMemoryTransport(this StandardConfigurer<ITransport> configurer, InMemNetwork network, string inputQueueName, string errorQueueName)
        {
            configurer.Register(context => new InMemTransport(network, inputQueueName));
        }
    }
}