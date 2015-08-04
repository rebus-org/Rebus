using System.Data;
using System.Threading.Tasks;
using Npgsql;

namespace Rebus.PostgreSql
{
    public class PostgresConnectionHelper
    {
        readonly string _connectionString;

        public PostgresConnectionHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<PostgresConnection> GetConnection()
        {
            var connection = new NpgsqlConnection(_connectionString);
            
            await connection.OpenAsync();

            var currentTransaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

            return new PostgresConnection(connection, currentTransaction);
        }
    }
}