using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Rebus.Persistence.SqlServer
{
    public class DbConnectionProvider
    {
        readonly string _connectionString;

        public DbConnectionProvider(string connectionString)
        {
            _connectionString = connectionString;

            IsolationLevel = IsolationLevel.ReadCommitted;
        }

        public async Task<DbConnection> GetConnection()
        {
            var connection = new SqlConnection(_connectionString);
            
            await connection.OpenAsync();

            return new DbConnection(connection, connection.BeginTransaction(IsolationLevel), false);
        }

        public IsolationLevel IsolationLevel { get; set; }
    }
}