using System;
using Rebus.Config;
using Rebus.Logging;

namespace Rebus.DataBus.FileSystem
{
    /// <summary>
    /// Provides extensions methods for configuring the file system storage for the data bus
    /// </summary>
    public static class FileSystemDataBusExtensions
    {
        /// <summary>
        /// Configures the data bus to store data in the file system
        /// </summary>
        public static void StoreInFileSystem(this StandardConfigurer<IDataBusStorage> configurer, string directoryPath)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));
            if (directoryPath == null) throw new ArgumentNullException(nameof(directoryPath));

            configurer.Register(c =>
            {
                var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();

                return new FileSystemDataBusStorage(directoryPath, rebusLoggerFactory);
            });
        }
    }
}