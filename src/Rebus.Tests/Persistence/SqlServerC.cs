using System.Configuration;
using System.Linq;
using NUnit.Framework;

namespace Rebus.Tests.Persistence
{
    public class SqlServerC
    {
        public static string ConnectionString
        {
            get
            {
                var connectionStringSettings = ConfigurationManager.ConnectionStrings
                    .Cast<ConnectionStringSettings>()
                    .FirstOrDefault();

                Assert.IsNotNull(connectionStringSettings,
                                 "There doesn't seem to be any connection strings in the app.config.");

                return connectionStringSettings.ConnectionString;
            }
        }
    }
}