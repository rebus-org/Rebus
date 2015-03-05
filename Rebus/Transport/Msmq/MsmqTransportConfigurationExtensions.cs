using Rebus.Config;

namespace Rebus.Transport.Msmq
{
    public static class MsmqTransportConfigurationExtensions
    {
        public static void UseMsmq(this StandardConfigurer<ITransport> configurer, string inputQueueName)
        {
            configurer.Register(context => new MsmqTransport(inputQueueName));
        }
    }
}