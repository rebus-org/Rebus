using System.Configuration;
using System.Linq;
using NUnit.Framework;

namespace Rebus.Tests.Persistence
{
    public static class ConnectionStrings
    {
        public static string SqlServer
        {
            get
            {
                var connectionStringSettings = ConfigurationManager.ConnectionStrings
                    .Cast<ConnectionStringSettings>()
                    .FirstOrDefault(x => x.Name == "LocalSqlServer");

                Assert.IsNotNull(connectionStringSettings,
                                 "There doesn't seem to be any connection strings in the app.config.");

                return connectionStringSettings.ConnectionString;
            }
        }

        public static string PostgreSql
        {
            get
            {
                var connectionStringSettings = ConfigurationManager.ConnectionStrings
                    .Cast<ConnectionStringSettings>()
                    .FirstOrDefault(x => x.Name == "LocalPostgreSql");

                Assert.IsNotNull(connectionStringSettings,
                                 "There doesn't seem to be any connection strings in the app.config.");

                return connectionStringSettings.ConnectionString;
            }
        }

        public static string MongoDb
        {
            get { return "mongodb://localhost:27017/rebus_test"; }
        }
    }
}