using System;
using Rebus.Config;

namespace Rebus.Sagas;

/// <summary>
/// Configuration extensions for additional saga-related things
/// </summary>
public static class SagaCorrelationConfigurationExtensions
{
    /// <summary>
    /// Adds the given <paramref name="correlationErrorHandler"/> as the <see cref="ICorrelationErrorHandler"/>, which gets invoked each time
    /// Rebus cannot correlate a message with an ongoing saga (and the given message is not allowed to start a new one)
    /// </summary>
    public static void UseCorrelationErrorHandler(this StandardConfigurer<ISagaStorage> configurer, ICorrelationErrorHandler correlationErrorHandler)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        if (correlationErrorHandler == null) throw new ArgumentNullException(nameof(correlationErrorHandler));

        configurer
            .OtherService<ICorrelationErrorHandler>()
            .Register(_ => correlationErrorHandler);
    }
}