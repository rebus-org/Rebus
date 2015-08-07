using System;
using Npgsql;
using Rebus.Tests;

namespace Rebus.PostgreSql.Tests
{
    public class PostgreSqlTestHelper
    {
        const string TableDoesNotExist = "42P01";
        static readonly PostgresConnectionHelper PostgresConnectionHelper = new PostgresConnectionHelper(ConnectionString);

        public static string DatabaseName
        {
            get { return string.Format("rebus2_test_{0}", TestConfig.Suffix).TrimEnd('_'); }
        }

        public static string ConnectionString
        {
            get { return GetConnectionStringForDatabase(DatabaseName); }
        }

        public static PostgresConnectionHelper ConnectionHelper
        {
            get { return PostgresConnectionHelper; }
        }

        public static void DropTable(string tableName)
        {
            using (var connection = PostgresConnectionHelper.GetConnection().Result)
            {
                using (var comand = connection.CreateCommand())
                {
                    comand.CommandText = string.Format(@"drop table ""{0}"";", tableName);

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
            return string.Format("Server=localhost;Database={0};User=postgres;Password=postgres;", databaseName);
        }
    }
}