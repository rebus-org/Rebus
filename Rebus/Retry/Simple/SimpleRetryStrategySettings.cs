using System;
using Rebus.Messages;

namespace Rebus.Retry.Simple;

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
    /// Default age in minutes of an in-mem error tracking, which will be used unless <see cref="ErrorTrackingMaxAgeMinutes"/> is set to something else.
    /// </summary>
    public const int DefaultErrorTrackingMaxAgeMinutes = 10;

    /// <summary>
    /// Creates the settings with the given error queue address and number of delivery attempts, defaulting to <see cref="DefaultErrorQueueName"/> and <see cref="DefaultNumberOfDeliveryAttempts"/> 
    /// as the error queue address and number of delivery attempts, respectively
    /// </summary>
    public SimpleRetryStrategySettings(
        string errorQueueAddress = DefaultErrorQueueName,
        int maxDeliveryAttempts = DefaultNumberOfDeliveryAttempts,
        bool secondLevelRetriesEnabled = false,
        int errorDetailsHeaderMaxLength = int.MaxValue,
        int errorTrackingMaxAgeMinutes = DefaultErrorTrackingMaxAgeMinutes
    )
    {
        if (errorDetailsHeaderMaxLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(errorDetailsHeaderMaxLength), errorDetailsHeaderMaxLength, "Please specify a non-negative number as the max length of the error details header");
        }
        if (maxDeliveryAttempts < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDeliveryAttempts), maxDeliveryAttempts, "Please specify a non-negative number as the number of delivery attempts");
        }
        if (errorTrackingMaxAgeMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(errorTrackingMaxAgeMinutes), errorTrackingMaxAgeMinutes, 
                "Please specify the max age in minutes of an in-mem error tracking before it gets purged (must be >= 1)");
        }
        ErrorQueueAddress = errorQueueAddress ?? throw new ArgumentException("Error queue address cannot be NULL");
        MaxDeliveryAttempts = maxDeliveryAttempts;
        SecondLevelRetriesEnabled = secondLevelRetriesEnabled;
        ErrorDetailsHeaderMaxLength = errorDetailsHeaderMaxLength;
        ErrorTrackingMaxAgeMinutes = errorTrackingMaxAgeMinutes;
    }

    /// <summary>
    /// Name of the error queue
    /// </summary>
    public string ErrorQueueAddress { get; set; }

    /// <summary>
    /// Number of attempted deliveries to make before moving the poisonous message to the error queue
    /// </summary>
    public int MaxDeliveryAttempts { get; set; }

    /// <summary>
    /// Configures whether an additional round of delivery attempts should be made with a <see cref="FailedMessageWrapper{TMessage}"/> wrapping the originally failed messageS
    /// </summary>
    public bool SecondLevelRetriesEnabled { get; set; }

    /// <summary>
    /// Configures the max length of the <see cref="Headers.ErrorDetails"/> header. Depending on the configured number of delivery attempts and the transport's capabilities, it might
    /// be necessary to truncate the value of this header.
    /// </summary>
    public int ErrorDetailsHeaderMaxLength { get; set; }

    /// <summary>
    /// Configures the maximum age in minutes of an in-mem error tracking.
    /// This is a safety precaution, because the in-mem error tracker can end up tracking messages that it never sees
    /// again if multiple bus instances are consuming messages from the same queue.
    /// </summary>
    public int ErrorTrackingMaxAgeMinutes { get; set; }
}