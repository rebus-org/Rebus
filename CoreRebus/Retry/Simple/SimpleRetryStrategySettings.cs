namespace Rebus.Retry.Simple
{
    /// <summary>
    /// Contains the settings used by <see cref="SimpleRetryStrategy"/>
    /// </summary>
    public class SimpleRetryStrategySettings
    {
        /// <summary>
        /// Name of the default error queue, which will be used unless <see cref="ErrorQueueAddress"/> is set to something else
        /// </summary>
        public const string DefaultErrorQueueName = "error";

        /// <summary>
        /// Number of delivery attempts that will be used unless <see cref="MaxDeliveryAttempts"/> is set to something else
        /// </summary>
        public const int DefaultNumberOfDeliveryAttempts = 5;

        /// <summary>
        /// Creates the settings with the given error queue address and number of delivery attempts, defaulting to <see cref="DefaultErrorQueueName"/> and <see cref="DefaultNumberOfDeliveryAttempts"/> 
        /// as the error queue address and number of delivery attempts, respectively
        /// </summary>
        public SimpleRetryStrategySettings(string errorQueueAddress = DefaultErrorQueueName, int maxDeliveryAttempts = DefaultNumberOfDeliveryAttempts, bool secondLevelRetriesEnabled = false)
        {
            ErrorQueueAddress = errorQueueAddress;
            MaxDeliveryAttempts = maxDeliveryAttempts;
            SecondLevelRetriesEnabled = secondLevelRetriesEnabled;
        }

        /// <summary>
        /// Name of the error queue
        /// </summary>
        public string ErrorQueueAddress { get; }

        /// <summary>
        /// Number of attempted deliveries to make before moving the poisonous message to the error queue
        /// </summary>
        public int MaxDeliveryAttempts { get; }

        /// <summary>
        /// Configures whether an additional round of delivery attempts should be made with a <see cref="FailedMessageWrapper{TMessage}"/> wrapping the originally failed messageS
        /// </summary>
        public bool SecondLevelRetriesEnabled { get; }
    }
}