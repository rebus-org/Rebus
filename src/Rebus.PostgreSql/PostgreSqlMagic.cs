using System.Collections.Generic;

using Npgsql;

namespace Rebus.PostgreSql
{
    internal static class PostgreSqlMagic
    {
        public static List<string> GetTableNames(this NpgsqlConnection connection, NpgsqlTransaction transaction = null)
        {
            var tableNames = new List<string>();

            using (var command = connection.CreateCommand())
            {
                if (transaction != null)
                {
                    command.Transaction = transaction;
                }

                command.CommandText = "select * from information_schema.tables where table_schema not in ('pg_catalog', 'information_schema')";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tableNames.Add(reader["table_name"].ToString());
                    }
                }
            }

            return tableNames;
        }
    }
}