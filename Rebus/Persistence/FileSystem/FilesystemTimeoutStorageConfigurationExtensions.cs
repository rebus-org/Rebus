using Rebus.Config;
using Rebus.Logging;
using Rebus.Timeouts;

namespace Rebus.Persistence.FileSystem
{
    /// <summary>
    /// Configures the bus to use the filesystem to store timeouts
    /// </summary>
    public static  class FilesystemTimeoutStorageConfigurationExtensions
    {
        /// <summary>
        /// Configures the bus to use the filesystem to store timeouts
        /// </summary>
        /// <param name="configurer">the rebus configuration</param>
        /// <param name="basePath">the path to store timeouts under</param>
        public static void UseFilesystem(this StandardConfigurer<ITimeoutManager> configurer, string basePath)
        {
            configurer.Register(c=>new FilesystemTimeoutManager(basePath, c.Get<IRebusLoggerFactory>()));
        }
    }
}