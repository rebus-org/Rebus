using System;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Sagas;

namespace Rebus.Persistence.FileSystem;

/// <summary>
/// Configures extensions for using the filesystem to store sagas
/// </summary>
public static class FileSystemSagaStorageConfigurationExtensions
{
    /// <summary>
    /// Configures Rebus to use the filesystem to store sagas. Please note that this way of storing saga data is not
    /// the most effective, and therefore it is probably best suited for testing and very simple and mild requirements
    /// </summary>
    public static void UseFilesystem(this StandardConfigurer<ISagaStorage> configurer, string basePath)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        if (basePath == null) throw new ArgumentNullException(nameof(basePath));

        configurer.Register(c => new FileSystemSagaStorage(basePath, c.Get<IRebusLoggerFactory>()));
    }
}