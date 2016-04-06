using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;

namespace Rebus.Retry.Simple
{
    /// <summary>
    /// Configuration extensions for the simple retry strategy
    /// </summary>
    public static class SimpleRetryStrategyConfigurationExtensions
    {
        /// <summary>
        /// Configures the simple retry strategy, using the specified error queue address and number of delivery attempts
        /// </summary>
        public static void SimpleRetryStrategy(this OptionsConfigurer optionsConfigurer,
            string errorQueueAddress = SimpleRetryStrategySettings.DefaultErrorQueueName,
            int maxDeliveryAttempts = SimpleRetryStrategySettings.DefaultNumberOfDeliveryAttempts,
            bool secondLevelRetriesEnabled = false)
        {
            optionsConfigurer.Register(c => new SimpleRetryStrategySettings(errorQueueAddress, maxDeliveryAttempts, secondLevelRetriesEnabled));

            if (secondLevelRetriesEnabled)
            {
                optionsConfigurer.Decorate<IPipeline>(c =>
                {
                    var pipeline = c.Get<IPipeline>();
                    var errorTracker = c.Get<IErrorTracker>();

                    var incomingStep = new FailedMessageWrapperStep(errorTracker);
                    var outgoingStep = new VerifyCannotSendFailedMessageWrapperStep();

                    return new PipelineStepInjector(pipeline)
                        .OnReceive(incomingStep, PipelineRelativePosition.After, typeof(DeserializeIncomingMessageStep))
                        .OnSend(outgoingStep, PipelineRelativePosition.Before, typeof(SerializeOutgoingMessageStep));
                });
            }
        }
    }
}