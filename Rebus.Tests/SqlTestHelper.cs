using System;
using System.Data.SqlClient;
using Rebus.Persistence.SqlServer;

namespace Rebus.Tests
{
    public class SqlTestHelper
    {
        static bool _databaseHasBeenInitialized;

        public static string ConnectionString
        {
            get
            {
                var databaseName = string.Format("rebus2_test_{0}", TestConfig.Suffix).TrimEnd('_');

                if (!_databaseHasBeenInitialized)
                {
                    InitializeDatabase(databaseName);
                }

                Console.WriteLine("Using local SQL database {0}", databaseName);

                return GetConnectionStringForDatabase(databaseName);
            }
        }

        static void InitializeDatabase(string databaseName)
        {
            var masterConnectionString = GetConnectionStringForDatabase("master");

            using (var connection = new SqlConnection(masterConnectionString))
            {
                connection.Open();

                if (connection.GetDatabaseNames().Contains(databaseName)) return;

                Console.WriteLine("Creating database {0}", databaseName);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format("CREATE DATABASE [{0}]", databaseName);
                    command.ExecuteNonQuery();
                }
            }

            _databaseHasBeenInitialized = true;
        }

        static string GetConnectionStringForDatabase(string databaseName)
        {
            return string.Format("server=.; database={0}; trusted_connection=true", databaseName);
        }

        public static void DropTable(string tableName)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                if (!connection.GetTableNames().Contains(tableName)) return;

                Console.WriteLine("Dropping table {0}", tableName);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format("DROP TABLE [{0}]", tableName);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}