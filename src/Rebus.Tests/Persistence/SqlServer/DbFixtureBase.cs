using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using NUnit.Framework;
using log4net.Config;

namespace Rebus.Tests.Persistence.SqlServer
{
    public class DbFixtureBase
    {
        static DbFixtureBase()
        {
            XmlConfigurator.Configure();
        }

        [SetUp]
        public void SetUp()
        {
            DoSetUp();
        }

        protected virtual void DoSetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
            DoTearDown();
        }

        protected virtual void DoTearDown()
        {
        }

        protected static string ConnectionString
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

        protected void DeleteRows(string tableName)
        {
            ExecuteCommand("delete from " + tableName);
        }

        static void ExecuteCommand(string commandText)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();

                using (var command = conn.CreateCommand())
                {
                    command.CommandText = commandText;
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}