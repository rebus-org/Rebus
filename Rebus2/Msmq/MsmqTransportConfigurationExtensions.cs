using Rebus2.Config;
using Rebus2.Transport;

namespace Rebus2.Msmq
{
    public static class MsmqTransportConfigurationExtensions
    {
        public static void UseMsmq(this StandardConfigurer<ITransport> configurer, string inputQueueName, string errorQueueName)
        {
            configurer.Register(context => new MsmqTransport(inputQueueName));
        }
    }
}