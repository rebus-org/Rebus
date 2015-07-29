using System.Collections.Generic;
using System.Data.SqlClient;

namespace Rebus.Persistence.SqlServer
{
    internal static class SqlServerMagic
    {
        public const int PrimaryKeyViolationNumber = 2627;

        public static List<string> GetTableNames(this SqlConnection connection, SqlTransaction transaction = null)
        {
            var tableNames = new List<string>();

            using (var command = connection.CreateCommand())
            {
                if (transaction != null)
                {
                    command.Transaction = transaction;
                }
                command.CommandText = "select * from sys.tables";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tableNames.Add(reader["name"].ToString());
                    }
                }
            }

            return tableNames;
        }
    }
}