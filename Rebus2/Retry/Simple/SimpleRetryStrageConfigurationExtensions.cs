using Rebus2.Config;

namespace Rebus2.Retry.Simple
{
    public static class SimpleRetryStrageConfigurationExtensions
    {
        public static void SimpleRetryStrategy(this OptionsConfigurer optionsConfigurer, string errorQueueAddress = null, int maxDeliveryAttempts = 5)
        {
            optionsConfigurer.Register(c => new SimpleRetryStrategySettings(errorQueueAddress, maxDeliveryAttempts));
        }
    }
}