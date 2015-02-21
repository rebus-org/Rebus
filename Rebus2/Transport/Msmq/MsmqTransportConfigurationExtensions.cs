using Rebus2.Config;

namespace Rebus2.Transport.Msmq
{
    public static class MsmqTransportConfigurationExtensions
    {
        public static void UseMsmq(this StandardConfigurer<ITransport> configurer, string inputQueueName, string errorQueueName)
        {
            configurer.Register(context => new MsmqTransport(inputQueueName));
        }
    }
}