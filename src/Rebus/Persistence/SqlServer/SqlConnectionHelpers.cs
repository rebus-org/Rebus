using System.Collections.Generic;
using System.Data.SqlClient;

namespace Rebus.Persistence.SqlServer
{
    /// <summary>
    /// Helpers that provides commonly needed functionality on an <see cref="SqlConnection"/>
    /// </summary>
    public static class SqlConnectionHelpers
    {
         /// <summary>
         /// Gets the names of all the tables of the current database
         /// </summary>
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