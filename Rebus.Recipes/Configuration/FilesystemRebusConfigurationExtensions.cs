using System.IO;
using Rebus.Config;
using Rebus.DataBus;
using Rebus.DataBus.FileSystem;
using Rebus.Logging;
using Rebus.Persistence.FileSystem;
using Rebus.Transport.FileSystem;

namespace Rebus.Recipes.Configuration
{
    public static class FilesystemRebusConfigruationExtensions
    {
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

        public static RebusConfigurer UseFileSystemAsOneWayClient(this RebusConfigurer configurer, string baseDirectory)
        {
            var transport = Path.Combine(baseDirectory, "transport");
            var sagas = Path.Combine(baseDirectory, "sagas");
            var timeouts = Path.Combine(baseDirectory, "timeouts");
            var subscriptions = Path.Combine(baseDirectory, "subscriptions.json");
            var dataBus = Path.Combine(baseDirectory, "databus");
            return configurer.Transport(t => t.UseFileSystemAsOneWayClient(transport))
                .Sagas(t => t.Register(c => new FilesystemSagaStorage(sagas, c.Get<IRebusLoggerFactory>()))) // is saga storage valid or one-way clients?
                .Subscriptions(t => t.UseJsonFile(subscriptions))
                .Timeouts(t => t.Register(c => new FilesystemTimeoutManager(timeouts, c.Get<IRebusLoggerFactory>())))
                .Options(o =>
                {
                    o.EnableDataBus().Register(r => new FileSystemDataBusStorage(dataBus, r.Get<IRebusLoggerFactory>()));
                });
        }
    }
}
