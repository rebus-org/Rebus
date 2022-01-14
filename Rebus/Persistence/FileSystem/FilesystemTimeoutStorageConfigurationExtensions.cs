using System;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Time;
using Rebus.Timeouts;

namespace Rebus.Persistence.FileSystem;

/// <summary>
/// Configures the bus to use the filesystem to store timeouts
/// </summary>
public static class FileSystemTimeoutStorageConfigurationExtensions
{
    /// <summary>
    /// Configures the bus to use the filesystem to store timeouts
    /// </summary>
    /// <param name="configurer">the rebus configuration</param>
    /// <param name="basePath">the path to store timeouts under</param>
    public static void UseFileSystem(this StandardConfigurer<ITimeoutManager> configurer, string basePath)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        if (basePath == null) throw new ArgumentNullException(nameof(basePath));

        configurer.Register(c => new FileSystemTimeoutManager(basePath, c.Get<IRebusLoggerFactory>(), c.Get<IRebusTime>()));
    }
}