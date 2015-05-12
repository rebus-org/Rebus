using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus.Persistence.SqlServer
{
    /// <summary>
    /// Implementation of <see cref="IDbConnectionProvider"/> that ensures that MARS (multiple active result sets) is enabled on the
    /// given connection string (possibly by enabling it by itself)
    /// </summary>
    public class DbConnectionProvider : IDbConnectionProvider
    {
        readonly string _connectionString;

        /// <summary>
        /// Wraps the connection string with the given name from app.config (if it is found), or interprets the given string as
        /// a connection string to use. Will use <see cref="System.Data.IsolationLevel.ReadCommitted"/> by default on transactions,
        /// unless another isolation level is set with the <see cref="IsolationLevel"/> property
        /// </summary>
        public DbConnectionProvider(string connectionStringOrConnectionStringName)
        {
            var connectionString = GetConnectionString(connectionStringOrConnectionStringName);

            _connectionString = EnsureMarsIsEnabled(connectionString);

            IsolationLevel = IsolationLevel.ReadCommitted;
        }

        string EnsureMarsIsEnabled(string connectionString)
        {
            var connectionStringParameters = connectionString.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(kvpString =>
                {
                    var tokens = kvpString.Split("=".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    return new
                    {
                        Key = tokens[0],
                        Value = string.Join("=", tokens.Skip(1))
                    };
                })
                .ToDictionary(a => a.Key, a => a.Value, StringComparer.InvariantCultureIgnoreCase);

            if (!connectionStringParameters.ContainsKey("MultipleActiveResultSets"))
            {
                return connectionString + ";MultipleActiveResultSets=true";
            }

            return connectionString;
        }

        string GetConnectionString(string connectionStringOrConnectionStringName)
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionStringOrConnectionStringName];

            if (connectionStringSettings != null)
                return connectionStringSettings.ConnectionString;

            return connectionStringOrConnectionStringName;
        }

        public async Task<IDbConnection> GetConnection()
        {
            var connection = new SqlConnection(_connectionString);
            
            await connection.OpenAsync();

            return new DbConnectionWrapper(connection, connection.BeginTransaction(IsolationLevel), false);
        }

        /// <summary>
        /// Gets/sets the isolation level used for transactions
        /// </summary>
        public IsolationLevel IsolationLevel { get; set; }
    }
}