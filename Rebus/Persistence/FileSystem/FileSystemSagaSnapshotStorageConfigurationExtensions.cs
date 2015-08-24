using Rebus.Auditing.Sagas;
using Rebus.Config;

namespace Rebus.Persistence.FileSystem
{
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
            configurer.Register(c => new FileSystemSagaSnapshotStorage(directory));
        }
    }
}