using System.Collections.Generic;
using System.Data.SqlClient;

namespace Rebus.Persistence.SqlServer
{
    public static class SqlConnectionHelpers
    {
         public static List<string> GetTableNames(this SqlConnection connection)
         {
             var tableNames = new List<string>();

             using (var command = connection.CreateCommand())
             {
                 command.CommandText = "select * from sys.Tables";

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