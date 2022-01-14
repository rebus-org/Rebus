using System;
using Rebus.Config;
using Rebus.Time;
using Rebus.Timeouts;

namespace Rebus.Persistence.InMem;

/// <summary>
/// Configuration extensions for in-mem timeout manager
/// </summary>
public static class InMemoryTimeoutManagerExtensions
{
    /// <summary>
    /// Configures Rebus to store timeouts in memory. Please note that this is probably not really suitable for production usage,
    /// as deferred messages will be lost in case the endpoint is restarted. Therefore, the in-mem timeout manager is probably
    /// mostly suited best to be used in automated tests.
    /// </summary>
    public static void StoreInMemory(this StandardConfigurer<ITimeoutManager> configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
            
        configurer.Register(c => new InMemoryTimeoutManager(c.Get<IRebusTime>()));
    }
}