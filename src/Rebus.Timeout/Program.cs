using System;
using System.IO;
using Rebus.Log4Net;
using Rebus.Logging;
using Rebus.Persistence.InMemory;
using Rebus.Persistence.SqlServer;
using Rebus.Timeout.Configuration;
using Topshelf;
using log4net.Config;

namespace Rebus.Timeout
{
    class Program
    {
        const string DefaultTimeoutsTableName = "RebusTimeoutManager";
        static ILog log;

        static Program()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        static void Main()
        {
            XmlConfigurator.ConfigureAndWatch(new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config")));

            RebusLoggerFactory.Current = new Log4NetLoggerFactory();

            HostFactory
                .Run(s =>
                         {
                             const string text = "Rebus Timeout Service";

                             s.SetDescription("Rebus Timeout Service - Install named instance by adding '/instance:\"myInstance\"' when installing.");
                             s.SetDisplayName(text);
                             s.SetInstanceName("default");
                             s.SetServiceName("rebus_timeout_service");

                             s.Service<TimeoutService>(c =>
                                                           {
                                                               c.ConstructUsing(CreateTimeoutService);
                                                               c.WhenStarted(t => t.Start());
                                                               c.WhenStopped(t => t.Stop());
                                                           });
                         });
        }

        static TimeoutService CreateTimeoutService()
        {
            try
            {
                var configuration = TimeoutConfigurationSection.GetSection();

                if (configuration == null)
                {
                    log.Warn("The timeout manager will use the in-memory timeout storage, which is NOT suitable for production use. For production use, you should use a SQL Server (e.g. a locally installed SQL Express).");

                    var storage = new InMemoryTimeoutStorage();
                    var timeoutService = new TimeoutService(storage);
                    
                    return timeoutService;
                }

                EnsureIsSet(configuration.InputQueue);
                EnsureIsSet(configuration.StorageType);

                switch (configuration.StorageType.ToLowerInvariant())
                {
                    case "sql":
                        log.Info("Using the SQL timeout storage - the default table name '{0}' will be used", DefaultTimeoutsTableName);
                        return new TimeoutService(new SqlServerTimeoutStorage(configuration.Parameters, DefaultTimeoutsTableName), configuration.InputQueue);

                    default:
                        throw new ArgumentException(
                            string.Format("Cannot use the value '{0}' as the storage type... sorry!",
                                          configuration.StorageType));
                }
            }
            catch(Exception e)
            {
                log.Error(e, "An error occurred while attempting to configure the timeout manager");
                throw;
            }
        }

        static void EnsureIsSet(string setting)
        {
            if (!string.IsNullOrWhiteSpace(setting)) return;

            throw new ArgumentException(string.Format(@"When you include the TimeoutConfigurationSection, you must specify input queue name, error queue name and a way to store the timeouts.

Take a look at this example configuration snippet

  <configSections>
    <section name=""timeout"" type=""Rebus.Timeout.Configuration.TimeoutConfigurationSection, Rebus.Timeout""/>
  </configSections>

  <timeout inputQueue=""rebus.timeout.input"" storageType=""SQL"" parameters=""server=.;initial catalog=RebusTimeoutManager;integrated security=sspi""/>

for inspiration on how it can be done.
"));
        }
    }
}
