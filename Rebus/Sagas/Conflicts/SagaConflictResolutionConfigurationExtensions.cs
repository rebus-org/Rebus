using System;
using Rebus.Config;

namespace Rebus.Sagas.Conflicts;

/// <summary>
/// Configuration extensions for customizing conflict resolution
/// </summary>
public static class SagaConflictResolutionConfigurationExtensions
{
    /// <summary>
    /// Sets maximum number of times conflict resolution is invoked. Only relevant in cases where <see cref="Saga{T}.ResolveConflict"/> is implemented in a
    /// saga handler. If the value is set to 0, conflict resolution is disabled even though the <see cref="Saga{T}.ResolveConflict"/> is implemented.
    /// </summary>
    public static void SetMaxConflictResolutionAttempts( this StandardConfigurer<ISagaStorage> configurer, int value)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Please specify a value greater than or equal to 0");

        configurer.OtherService<Options>().Decorate(c =>
        {
            var options = c.Get<Options>();
            options.MaxConflictResolutionAttempts = value;
            return options;
        });
    }
}