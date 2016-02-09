using System;
using Npgsql;
using Rebus.Tests;

namespace Rebus.PostgreSql.Tests
{
    public class PostgreSqlTestHelper
    {
        const string TableDoesNotExist = "42P01";
        static readonly PostgresConnectionHelper PostgresConnectionHelper = new PostgresConnectionHelper(ConnectionString);

        public static string DatabaseName => $"rebus2_test_{TestConfig.Suffix}".TrimEnd('_');

        public static string ConnectionString => GetConnectionStringForDatabase(DatabaseName);

        public static PostgresConnectionHelper ConnectionHelper => PostgresConnectionHelper;

        public static void DropTable(string tableName)
        {
            using (var connection = PostgresConnectionHelper.GetConnection().Result)
            {
                using (var comand = connection.CreateCommand())
                {
                    comand.CommandText = $@"drop table ""{tableName}"";";

                    try
                    {
                        comand.ExecuteNonQuery();

                        Console.WriteLine("Dropped postgres table '{0}'", tableName);
                    }
                    catch (NpgsqlException exception)
                    {
                        if (exception.Code != TableDoesNotExist) throw;
                    }
                }

                connection.Complete();
            }
        }

        static string GetConnectionStringForDatabase(string databaseName)
        {
            return Environment.GetEnvironmentVariable("REBUS_POSTGRES")
                   ?? $"Server=localhost;Database={databaseName};User=postgres;Password=postgres;";
        }
    }
}