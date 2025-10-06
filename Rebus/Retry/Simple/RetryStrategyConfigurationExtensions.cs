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
public static class RetryStrategyConfigurationExtensions
{
    /// <summary>
    /// Configures the simple retry strategy, using the specified error queue address and number of delivery attempts
    /// </summary>
    /// <param name="optionsConfigurer">(extension method target)</param>
    /// <param name="errorQueueName">Specifies the name of the error queue</param>
    /// <param name="maxDeliveryAttempts">Specifies how many delivery attempts should be made before forwarding a failed message to the error queue</param>
    /// <param name="secondLevelRetriesEnabled">Specifies whether second level retries should be enabled - when enabled, the message will be dispatched wrapped in an <see cref="IFailed{TMessage}"/> after the first <paramref name="maxDeliveryAttempts"/> delivery attempts, allowing a different handler to handle the message. Dispatch of the <see cref="IFailed{TMessage}"/> is subject to the same <paramref name="maxDeliveryAttempts"/> delivery attempts</param>
    /// <param name="errorDetailsHeaderMaxLength">Specifies a MAX length of the error details to be enclosed as the <see cref="Headers.ErrorDetails"/> header. As the enclosed error details can sometimes become very long (especially when using many delivery attempts), depending on the transport's capabilities it might sometimes be necessary to truncate the error details</param>
    /// <param name="errorTrackingMaxAgeMinutes">Specifies the max age of in-mem error trackings, for tracked messages that have not had any activity registered on them.</param>
    /// <param name="errorQueueErrorCooldownTimeSeconds">Specifies the time in seconds that the bus instance will wait if forwarding to the dead-letter queue fails.</param>
    /// <param name="errorHandlerMode">Specifies whether to pass a failed message to the error handler <see cref="ErrorHandlerMode.Immediately"/> or on the <see cref="ErrorHandlerMode.NextDelivery"/></param>
    public static void RetryStrategy(this OptionsConfigurer optionsConfigurer,
        string errorQueueName = RetryStrategySettings.DefaultErrorQueueName,
        int maxDeliveryAttempts = RetryStrategySettings.DefaultNumberOfDeliveryAttempts,
        bool secondLevelRetriesEnabled = false,
        int errorDetailsHeaderMaxLength = int.MaxValue,
        int errorTrackingMaxAgeMinutes = RetryStrategySettings.DefaultErrorTrackingMaxAgeMinutes,
        int errorQueueErrorCooldownTimeSeconds = RetryStrategySettings.DefaultErrorQueueErrorCooldownTimeSeconds,
        ErrorHandlerMode errorHandlerMode = ErrorHandlerMode.Immediately
    )
    {
        if (optionsConfigurer == null) throw new ArgumentNullException(nameof(optionsConfigurer));

        optionsConfigurer.Register(_ =>
        {
            var settings = new RetryStrategySettings(
                errorQueueName,
                maxDeliveryAttempts,
                secondLevelRetriesEnabled,
                errorDetailsHeaderMaxLength,
                errorTrackingMaxAgeMinutes,
                errorQueueErrorCooldownTimeSeconds: errorQueueErrorCooldownTimeSeconds,
                errorHandlerMode: errorHandlerMode
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