using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Rebus.Logging;
using IsolationLevel = System.Data.IsolationLevel;

#pragma warning disable 1998

namespace Rebus.Persistence.SqlServer
{
    /// <summary>
    /// Implementation of <see cref="IDbConnectionProvider"/> that ensures that MARS (multiple active result sets) is enabled on the
    /// given connection string (possibly by enabling it by itself)
    /// </summary>
    public class DbConnectionProvider : IDbConnectionProvider
    {
        readonly Func<SqlConnection> _connectionFactory;
        readonly ILog _log;
        bool _loggedAboutMars = false;

        /// <summary>
        /// Wraps the connection string with the given name from app.config (if it is found), or interprets the given string as
        /// a connection string to use. Will use <see cref="System.Data.IsolationLevel.ReadCommitted"/> by default on transactions,
        /// unless another isolation level is set with the <see cref="IsolationLevel"/> property
        /// </summary>
        public DbConnectionProvider(string connectionStringOrConnectionStringName, IRebusLoggerFactory rebusLoggerFactory)
        {
            if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));

            _log = rebusLoggerFactory.GetCurrentClassLogger();

            var connectionString = GetConnectionString(connectionStringOrConnectionStringName);

            connectionString = EnsureMarsIsEnabled(connectionString);

            _connectionFactory = () => new SqlConnection(connectionString);

            IsolationLevel = IsolationLevel.ReadCommitted;
        }

        /// <summary>
        /// Uses provided SqlConnection factory as constructor for SqlConnection used. Will use <see cref="System.Data.IsolationLevel.ReadCommitted"/> by default on transactions,
        /// unless another isolation level is set with the <see cref="IsolationLevel"/> property
        /// </summary>
        public DbConnectionProvider(Func<SqlConnection> connectionFactory, IRebusLoggerFactory rebusLoggerFactory)
        {
            if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));

            _log = rebusLoggerFactory.GetCurrentClassLogger();

            IsolationLevel = IsolationLevel.ReadCommitted;

            _connectionFactory = connectionFactory;
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
                if (!_loggedAboutMars)
                {
                    _log.Info("Supplied connection string does not have MARS enabled - the connection string will be modified to enable MARS!");
                    _loggedAboutMars = true;
                }

                return connectionString + ";MultipleActiveResultSets=true";
            }

            return connectionString;
        }

        static string GetConnectionString(string connectionStringOrConnectionStringName)
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
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    connection = _connectionFactory();
                    EnsureMarsIsEnabled(connection);

                    // do not use Async here! it would cause the tx scope to be disposed on another thread than the one that created it
                    connection.Open();
                }

                var transaction = connection.BeginTransaction(IsolationLevel);

                return new DbConnectionWrapper(connection, transaction, false);
            }
            catch (Exception exception)
            {
                _log.Warn("Could not open connection and begin transaction: {0}", exception);
                connection?.Dispose();
                throw;
            }
        }

        private void EnsureMarsIsEnabled(SqlConnection connection)
        {
            connection.ConnectionString = EnsureMarsIsEnabled(connection.ConnectionString);
        }

        /// <summary>
        /// Gets/sets the isolation level used for transactions
        /// </summary>
        public IsolationLevel IsolationLevel { get; set; }
    }
}