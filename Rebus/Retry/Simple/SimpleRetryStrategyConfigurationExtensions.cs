using System;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;

namespace Rebus.Retry.Simple;

/// <summary>
/// Configuration extensions for the simple retry strategy
/// </summary>
public static class SimpleRetryStrategyConfigurationExtensions
{
    /// <summary>
    /// Configures the simple retry strategy, using the specified error queue address and number of delivery attempts
    /// </summary>
    /// <param name="optionsConfigurer">(extension method target)</param>
    /// <param name="errorQueueAddress">Specifies the name of the error queue</param>
    /// <param name="maxDeliveryAttempts">Specifies how many delivery attempts should be made before forwarding a failed message to the error queue</param>
    /// <param name="secondLevelRetriesEnabled">Specifies whether second level retries should be enabled - when enabled, the message will be dispatched wrapped in an <see cref="IFailed{TMessage}"/> after the first <paramref name="maxDeliveryAttempts"/> delivery attempts, allowing a different handler to handle the message. Dispatch of the <see cref="IFailed{TMessage}"/> is subject to the same <paramref name="maxDeliveryAttempts"/> delivery attempts</param>
    /// <param name="errorDetailsHeaderMaxLength">Specifies a MAX length of the error details to be enclosed as the <see cref="Headers.ErrorDetails"/> header. As the enclosed error details can sometimes become very long (especially when using many delivery attempts), depending on the transport's capabilities it might sometimes be necessary to truncate the error details</param>
    /// <param name="errorTrackingMaxAgeMinutes">Specifies the max age of in-mem error trackings, for tracked messages that have not had any activity registered on them.</param>
    public static void SimpleRetryStrategy(this OptionsConfigurer optionsConfigurer,
        string errorQueueAddress = SimpleRetryStrategySettings.DefaultErrorQueueName,
        int maxDeliveryAttempts = SimpleRetryStrategySettings.DefaultNumberOfDeliveryAttempts,
        bool secondLevelRetriesEnabled = false,
        int errorDetailsHeaderMaxLength = int.MaxValue,
        int errorTrackingMaxAgeMinutes = SimpleRetryStrategySettings.DefaultErrorTrackingMaxAgeMinutes
    )
    {
        if (optionsConfigurer == null) throw new ArgumentNullException(nameof(optionsConfigurer));

        optionsConfigurer.Register(c =>
        {
            var settings = new SimpleRetryStrategySettings(
                errorQueueAddress,
                maxDeliveryAttempts,
                secondLevelRetriesEnabled,
                errorDetailsHeaderMaxLength,
                errorTrackingMaxAgeMinutes
            );

            return settings;
        });

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