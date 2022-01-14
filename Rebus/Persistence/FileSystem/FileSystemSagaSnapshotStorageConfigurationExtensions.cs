using Rebus.Auditing.Sagas;
using Rebus.Config;
using Rebus.Logging;
// ReSharper disable UnusedMember.Global

namespace Rebus.Persistence.FileSystem;

/// <summary>
/// Configuration extensions for the <see cref="FileSystemSagaSnapshotStorage"/>
/// </summary>
public static class FileSystemSagaSnapshotStorageConfigurationExtensions
{
    /// <summary>
    /// Configures Rebus to use JSON files to store snapshots of saga data
    /// </summary>
    public static void UseJsonFile(this StandardConfigurer<ISagaSnapshotStorage> configurer, string directory)
    {
        configurer.Register(c =>
        {
            var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
            return new FileSystemSagaSnapshotStorage(directory, rebusLoggerFactory);
        });
    }
}