using System.Collections.Generic;
using System.Data.SqlClient;

namespace Rebus.Persistence.SqlServer
{
    /// <summary>
    /// Wraps some nice extension methods for <see cref="SqlConnection"/> that makes it easy e.g. to query the schema
    /// </summary>
    public static class SqlServerMagic
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

                command.CommandText = string.Format("SELECT [name] FROM {0}", systemTableName);

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