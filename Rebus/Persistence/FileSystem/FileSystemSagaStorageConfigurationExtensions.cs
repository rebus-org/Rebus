using Rebus.Config;
using Rebus.Logging;
using Rebus.Sagas;

namespace Rebus.Persistence.FileSystem
{
    /// <summary>
    /// Configures extensions for using the filesystem to store sagas
    /// </summary>
    public static class FileSystemSagaStorageConfigurationExtensions
    {
        /// <summary>
        /// Use the filesystem to store sagas
        /// </summary>
        /// <param name="configurer">the rebus configuration</param>
        /// <param name="basePath">the path to store sagas under</param>
        public static void UseFilesystem(this StandardConfigurer<ISagaStorage> configurer, string basePath)
        {
            configurer.Register(c=>new FilesystemSagaStorage(basePath, c.Get<IRebusLoggerFactory>()));
        }
    }
}