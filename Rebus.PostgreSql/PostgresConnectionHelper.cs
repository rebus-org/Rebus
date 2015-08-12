using System.Data;
using System.Threading.Tasks;
using Npgsql;

namespace Rebus.PostgreSql
{
    /// <summary>
    /// Helps with managing <see cref="NpgsqlConnection"/>s
    /// </summary>
    public class PostgresConnectionHelper
    {
        readonly string _connectionString;

        /// <summary>
        /// Constructs this thingie
        /// </summary>
        public PostgresConnectionHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Gets a fresh, open and ready-to-use connection wrapper
        /// </summary>
        public async Task<PostgresConnection> GetConnection()
        {
            var connection = new NpgsqlConnection(_connectionString);
            
            await connection.OpenAsync();

            var currentTransaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

            return new PostgresConnection(connection, currentTransaction);
        }
    }
}