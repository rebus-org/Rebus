using Rebus.Config;

namespace Rebus.Retry.Simple
{
    /// <summary>
    /// 
    /// </summary>
    public static class SimpleRetryStrageConfigurationExtensions
    {
        /// <summary>
        /// Configures the simple retry strategy, using the specified error queue address and number of delivery attempts
        /// </summary>
        public static void SimpleRetryStrategy(this OptionsConfigurer optionsConfigurer,
            string errorQueueAddress = SimpleRetryStrategySettings.DefaultErrorQueueName,
            int maxDeliveryAttempts = SimpleRetryStrategySettings.DefaultNumberOfDeliveryAttempts)
        {
            optionsConfigurer.Register(c => new SimpleRetryStrategySettings(errorQueueAddress, maxDeliveryAttempts));
        }
    }
}