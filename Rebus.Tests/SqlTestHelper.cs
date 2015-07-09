using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Rebus.Persistence.SqlServer;

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

        public static string DatabaseName
        {
            get { return string.Format("rebus2_test_{0}", TestConfig.Suffix).TrimEnd('_'); }
        }

        public static void DropTable(string tableName)
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    if (!connection.GetTableNames().Contains(tableName, StringComparer.InvariantCultureIgnoreCase)) return;

                    Console.WriteLine("Dropping table {0}", tableName);

                    try
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = string.Format("DROP TABLE [{0}]", tableName);
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

                throw new ApplicationException(string.Format("Could not drop table '{0}'", tableName), exception);
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
                    who.Select(d => string.Join(", ", d.Select(kvp => string.Format("{0} = {1}", kvp.Key, kvp.Value))))));

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