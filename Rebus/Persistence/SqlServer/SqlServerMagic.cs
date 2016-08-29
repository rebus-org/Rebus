using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Rebus.Persistence.SqlServer
{
    /// <summary>
    /// Wraps some nice extension methods for <see cref="SqlConnection"/> that makes it easy e.g. to query the schema
    /// </summary>
    static class SqlServerMagic
    {
        /// <summary>
        /// Error code that is emitted on PK violations
        /// </summary>
        public const int PrimaryKeyViolationNumber = 2627;

        /// <summary>
        /// Error code that is emitted when something does not exist or the login's permissions do not allow the client to see it
        /// </summary>
        public const int ObjectDoesNotExistOrNoPermission = 3701;

        /// <summary>
        /// Gets the names of all tables in the current database
        /// </summary>
        public static List<string> GetTableNames(this SqlConnection connection, SqlTransaction transaction = null)
        {
            return GetNamesFrom(connection, transaction, "sys.tables");
        }

        /// <summary>
        /// Gets the names of all indexes in the current database
        /// </summary>
        public static List<string> GetIndexNames(this SqlConnection connection, SqlTransaction transaction = null)
        {
            return GetNamesFrom(connection, transaction, "sys.indexes");
        }

        /// <summary>
        /// Gets the names of all tables in the current database
        /// </summary>
        public static Dictionary<string, SqlDbType> GetColumns(this SqlConnection connection, string tableName, SqlTransaction transaction = null)
        {
            var results = new Dictionary<string, SqlDbType>();

            using (var command = connection.CreateCommand())
            {
                if (transaction != null)
                {
                    command.Transaction = transaction;
                }

                command.CommandText = $"SELECT [COLUMN_NAME] AS 'name', [DATA_TYPE] AS 'type' FROM [INFORMATION_SCHEMA].[COLUMNS] WHERE [TABLE_NAME] = '{tableName}'";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = (string)reader["name"];
                        var typeString = (string)reader["type"];
                        var type = GetDbType(typeString);

                        results[name] = type;
                    }
                }
            }

            return results;
        }

        static SqlDbType GetDbType(string typeString)
        {
            try
            {
                return (SqlDbType)Enum.Parse(typeof(SqlDbType), typeString, true);
            }
            catch (Exception exception)
            {
                throw new FormatException($"Could not parse '{typeString}' into {typeof(SqlDbType)}", exception);
            }
        }

        /// <summary>
        /// Gets the names of all databases on the current server
        /// </summary>
        public static List<string> GetDatabaseNames(this SqlConnection connection, SqlTransaction transaction = null)
        {
            return GetNamesFrom(connection, transaction, "sys.databases");
        }

        static List<string> GetNamesFrom(SqlConnection connection, SqlTransaction transaction, string systemTableName)
        {
            var names = new List<string>();

            using (var command = connection.CreateCommand())
            {
                if (transaction != null)
                {
                    command.Transaction = transaction;
                }

                command.CommandText = $"SELECT [name] FROM {systemTableName}";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = reader["name"].ToString();

                        names.Add(name);
                    }
                }
            }

            return names;
        }
    }
}