using System;
using Rebus.Config;

namespace Rebus.Timeouts;

/// <summary>
/// Configuration extensions for timeouts
/// </summary>
public static class TimeoutsConfigurationExtensions
{
    /// <summary>
    /// Configures Rebus to use another endpoint as the timeout manager
    /// </summary>
    public static void UseExternalTimeoutManager(this StandardConfigurer<ITimeoutManager> configurer, string timeoutManagerAddress)
    {
        if (string.IsNullOrWhiteSpace(timeoutManagerAddress))
        {
            throw new ArgumentException($"Cannot use '{timeoutManagerAddress}' as an external timeout manager address!", nameof(timeoutManagerAddress));
        }

        var options = configurer.Options;

        if (!string.IsNullOrWhiteSpace(options.ExternalTimeoutManagerAddressOrNull))
        {
            throw new InvalidOperationException(
                $"Cannot set external timeout manager address to '{timeoutManagerAddress}' because it has already been set to '{options.ExternalTimeoutManagerAddressOrNull}' - please set it only once!  (this operation COULD have been accepted, but it is probably an indication of an error in your configuration code that this value is configured twice, so we figured it was best to let you know)");
        }

        configurer.Register(c => new ThrowingTimeoutManager());
        options.ExternalTimeoutManagerAddressOrNull = timeoutManagerAddress;
    }
}