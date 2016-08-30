using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Rebus.Persistence.SqlServer;
using Rebus.Tests.Contracts;

namespace Rebus.Tests
{
    public class SqlTestHelper
    {
        static bool _databaseHasBeenInitialized;

        static string _connectionString;

        public static string ConnectionString
        {
            get
            {
                if (_connectionString != null)
                {
                    return _connectionString;
                }

                var databaseName = DatabaseName;

                if (!_databaseHasBeenInitialized)
                {
                    InitializeDatabase(databaseName);
                }

                Console.WriteLine("Using local SQL database {0}", databaseName);

                _connectionString = GetConnectionStringForDatabase(databaseName);

                return _connectionString;
            }
        }

        public static string DatabaseName => $"rebus2_test_{TestConfig.Suffix}".TrimEnd('_');

        public static void Execute(string sql)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void DropTable(string tableName)
        {
            DropObject($"DROP TABLE [{tableName}]", connection =>
            {
                var tableNames = connection.GetTableNames();

                return tableNames.Contains(tableName, StringComparer.InvariantCultureIgnoreCase);
            });
        }

        public static void DropIndex(string tableName, string indexName)
        {
            DropObject($"DROP INDEX [{indexName}] ON [{tableName}]", connection =>
            {
                var indexNames = connection.GetIndexNames();

                return indexNames.Contains(indexName, StringComparer.InvariantCultureIgnoreCase);
            });
        }

        static void DropObject(string sqlCommand, Func<SqlConnection, bool> executeCriteria)
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    var shouldExecute = executeCriteria(connection);
                    if (!shouldExecute) return;

                    try
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = sqlCommand;
                            command.ExecuteNonQuery();
                        }
                    }
                    catch (SqlException exception)
                    {
                        if (exception.Number == SqlServerMagic.ObjectDoesNotExistOrNoPermission) return;

                        throw;
                    }
                }
            }
            catch (Exception exception)
            {
                DumpWho();

                throw new ApplicationException($"Could not execute '{sqlCommand}'", exception);
            }
        }


        static void DumpWho()
        {
            try
            {
                Console.WriteLine("Trying to dump all active connections for db {0}...", DatabaseName);
                Console.WriteLine();

                var who = ExecSpWho()
                    .Where(kvp => kvp.ContainsKey("dbname"))
                    .Where(kvp => kvp["dbname"].Equals(DatabaseName, StringComparison.InvariantCultureIgnoreCase));

                Console.WriteLine(string.Join(Environment.NewLine,
                    who.Select(d => string.Join(", ", d.Select(kvp => $"{kvp.Key} = {kvp.Value}")))));

                Console.WriteLine();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Could not execute sp_who: {0}", exception);
            }
        }

        public static IEnumerable<IDictionary<string, string>> ExecSpWho()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "sp_who;";

                    using (var reader = command.ExecuteReader())
                    {
                        var rows = new List<Dictionary<string, string>>();

                        while (reader.Read())
                        {
                            rows.Add(Enumerable.Range(0, reader.FieldCount)
                                .Select(field => new
                                {
                                    ColumnName = reader.GetName(field),
                                    Value = (reader.GetValue(field) ?? "").ToString().Trim()
                                })
                                .ToDictionary(a => a.ColumnName, a => a.Value));
                        }

                        return rows;
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

                    if (connection.GetDatabaseNames().Contains(databaseName, StringComparer.InvariantCultureIgnoreCase)) return;

                    Console.WriteLine("Creating database {0}", databaseName);

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"CREATE DATABASE [{databaseName}]";
                        command.ExecuteNonQuery();
                    }
                }

                _databaseHasBeenInitialized = true;

            }
            catch (Exception exception)
            {
                throw new ApplicationException($"Could not initialize database '{databaseName}'", exception);
            }
        }

        static string GetConnectionStringForDatabase(string databaseName)
        {
            return Environment.GetEnvironmentVariable("REBUS_SQLSERVER")
                   ?? $"server=.; database={databaseName}; trusted_connection=true;";
        }
    }
}