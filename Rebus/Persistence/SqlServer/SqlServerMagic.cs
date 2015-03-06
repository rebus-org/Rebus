using System.Collections.Generic;
using System.Data.SqlClient;

namespace Rebus.Persistence.SqlServer
{
    public static class SqlServerMagic
    {
        public const int PrimaryKeyViolationNumber = 2627;

        public static List<string> GetTableNames(this SqlConnection connection, SqlTransaction transaction = null)
        {
            return GetNamesFrom(connection, transaction, "sys.tables");
        }

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

                command.CommandText = string.Format("SELECT * FROM {0}", systemTableName);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        names.Add(reader["name"].ToString());
                    }
                }
            }

            return names;
        }
    }
}