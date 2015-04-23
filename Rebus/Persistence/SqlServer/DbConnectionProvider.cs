using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Rebus.Persistence.SqlServer
{
    public class DbConnectionProvider
    {
        readonly string _connectionString;

        public DbConnectionProvider(string connectionStringOrConnectionStringName)
        {
            _connectionString = GetConnectionString(connectionStringOrConnectionStringName);

            IsolationLevel = IsolationLevel.ReadCommitted;
        }

        string GetConnectionString(string connectionStringOrConnectionStringName)
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionStringOrConnectionStringName];

            if (connectionStringSettings != null)
                return connectionStringSettings.ConnectionString;

            return connectionStringOrConnectionStringName;
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