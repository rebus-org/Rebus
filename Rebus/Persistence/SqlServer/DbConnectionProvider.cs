using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Logging;
#pragma warning disable 1998

namespace Rebus.Persistence.SqlServer
{
    /// <summary>
    /// Implementation of <see cref="IDbConnectionProvider"/> that ensures that MARS (multiple active result sets) is enabled on the
    /// given connection string (possibly by enabling it by itself)
    /// </summary>
    public class DbConnectionProvider : IDbConnectionProvider
    {
        readonly string _connectionString;
        readonly ILog _log;

        /// <summary>
        /// Wraps the connection string with the given name from app.config (if it is found), or interprets the given string as
        /// a connection string to use. Will use <see cref="System.Data.IsolationLevel.ReadCommitted"/> by default on transactions,
        /// unless another isolation level is set with the <see cref="IsolationLevel"/> property
        /// </summary>
        public DbConnectionProvider(string connectionStringOrConnectionStringName, IRebusLoggerFactory rebusLoggerFactory)
        {
            if (rebusLoggerFactory == null) throw new ArgumentNullException("rebusLoggerFactory");

            _log = rebusLoggerFactory.GetCurrentClassLogger();

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
                _log.Info("Supplied connection string does not have MARS enabled - the connection string will be modified to enable MARS!");
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

        /// <summary>
        /// Gets a nice ready-to-use database connection with an open transaction
        /// </summary>
        public async Task<IDbConnection> GetConnection()
        {
            SqlConnection connection = null;

            try
            {
                connection = new SqlConnection(_connectionString);

                connection.Open();

                var transaction = connection.BeginTransaction(IsolationLevel);

                return new DbConnectionWrapper(connection, transaction, false);
            }
            catch(Exception exception)
            {
                _log.Warn("Could not open connection and begin transaction: {0}", exception);
                if (connection != null)
                {
                    connection.Dispose();
                }
                throw;
            }
        }

        /// <summary>
        /// Gets/sets the isolation level used for transactions
        /// </summary>
        public IsolationLevel IsolationLevel { get; set; }
    }
}