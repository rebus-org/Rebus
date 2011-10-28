using System.Data.SqlClient;

namespace Rebus.Tests.Persistence.SqlServer
{
    public class DbFixtureBase
    {
        protected const string ConnectionString = "data source=.;integrated security=sspi;initial catalog=rebus_test";

        protected void DeleteRows(string tableName)
        {
            ExecuteCommand("delete from " + tableName);
        }

        static void ExecuteCommand(string commandText)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();

                using (var command = conn.CreateCommand())
                {
                    command.CommandText = commandText;
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}