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

        public static void DropTable(string tableName)
        {
            try
            {
                WithRetries(5, () =>
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
                });
            }
            catch (Exception exception)
            {
                throw new ApplicationException(string.Format("Could not drop table '{0}'", tableName), exception);
            }
        }

        static void WithRetries(int maxAttempts, Action action)
        {
            while (true)
            {
                try
                {
                    action();

                    return;
                }
                catch
                {
                    maxAttempts--;

                    Console.WriteLine("Remainint attempts: {0}", maxAttempts);

                    if (maxAttempts <= 0)
                    {
                        throw;
                    }
                }
            }
        }

        static void InitializeDatabase(string databaseName)
        {
            try
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
            catch (Exception exception)
            {
                throw new ApplicationException(string.Format("Could not initialize database '{0}'", databaseName), exception);
            }
        }

        static string GetConnectionStringForDatabase(string databaseName)
        {
            return string.Format("server=.; database={0}; trusted_connection=true;", databaseName);
        }
    }
}