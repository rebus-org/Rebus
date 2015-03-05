namespace Rebus.Retry.Simple
{
    public class SimpleRetryStrategySettings
    {
        public const string DefaultErrorQueueName = "error";

        public SimpleRetryStrategySettings(string errorQueueAddress = null, int maxDeliveryAttempts = 5)
        {
            MaxDeliveryAttempts = maxDeliveryAttempts;
            ErrorQueueAddress = errorQueueAddress ?? DefaultErrorQueueName;
        }

        public string ErrorQueueAddress { get; private set; }

        public int MaxDeliveryAttempts { get; private set; }
    }
}