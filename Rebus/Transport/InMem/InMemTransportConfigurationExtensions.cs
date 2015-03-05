using Rebus.Config;

namespace Rebus.Transport.InMem
{
    public static class InMemTransportConfigurationExtensions
    {
        public static void UseInMemoryTransport(this StandardConfigurer<ITransport> configurer, InMemNetwork network, string inputQueueName)
        {
            configurer.Register(context => new InMemTransport(network, inputQueueName));
        }
    }
}