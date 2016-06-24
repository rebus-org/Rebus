using System.IO;
using Rebus.Config;
using Rebus.DataBus;
using Rebus.DataBus.FileSystem;
using Rebus.Logging;
using Rebus.Persistence.FileSystem;
using Rebus.Transport.FileSystem;

namespace Rebus.Recipes.Configuration
{
    /// <summary>
    /// Configures rebus to use the filesystem store for everything.
    /// </summary>
    public static class FilesystemRebusConfigruationExtensions
    {
        /// <summary>
        /// Configures rebus to use the filesystem store for everything.
        /// </summary>
        /// <param name="config">The rebus configuration</param>
        /// <param name="baseDirectory">The directory to store everything under</param>
        /// <param name="queueName">The name of the rebus queue for this bus instance</param>
        /// <returns></returns>
        public static RebusConfigurer UseTheFilesystem(this RebusConfigurer config, string baseDirectory, string queueName)
        {
            var transport = Path.Combine(baseDirectory, "transport");
            var sagas = Path.Combine(baseDirectory, "sagas");
            var timeouts = Path.Combine(baseDirectory, "timeouts");
            var subscriptions = Path.Combine(baseDirectory, "subscriptions.json");
            var dataBus = Path.Combine(baseDirectory, "databus");
            return config.Transport(t => t.UseFileSystem(transport, queueName))
                .Sagas(t => t.Register(c => new FilesystemSagaStorage(sagas, c.Get<IRebusLoggerFactory>())))
                .Subscriptions(t => t.UseJsonFile(subscriptions))
                .Timeouts(t => t.Register(c => new FilesystemTimeoutManager(timeouts, c.Get<IRebusLoggerFactory>())))
                .Options(o =>
                {
                    o.EnableDataBus().Register(r => new FileSystemDataBusStorage(dataBus, r.Get<IRebusLoggerFactory>()));
                });
        }
        /// <summary>
        /// Configures rebus to use the filesystem store for everything in one-way client mode.
        /// </summary>
        /// <param name="config">The rebus configuration</param>
        /// <param name="baseDirectory">The directory to store everything under</param>
        public static RebusConfigurer UseFileSystemAsOneWayClient(this RebusConfigurer configurer, string baseDirectory)
        {
            var transport = Path.Combine(baseDirectory, "transport");
            var sagas = Path.Combine(baseDirectory, "sagas");
            var timeouts = Path.Combine(baseDirectory, "timeouts");
            var subscriptions = Path.Combine(baseDirectory, "subscriptions.json");
            var dataBus = Path.Combine(baseDirectory, "databus");
            return configurer.Transport(t => t.UseFileSystemAsOneWayClient(transport))
                .Subscriptions(t => t.UseJsonFile(subscriptions))
                .Timeouts(t => t.Register(c => new FilesystemTimeoutManager(timeouts, c.Get<IRebusLoggerFactory>())))
                .Options(o =>
                {
                    o.EnableDataBus().Register(r => new FileSystemDataBusStorage(dataBus, r.Get<IRebusLoggerFactory>()));
                });
        }
    }
}
