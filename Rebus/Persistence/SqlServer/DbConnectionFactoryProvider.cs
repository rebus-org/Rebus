using System;
using System.Threading.Tasks;
using Rebus.Logging;
using IsolationLevel = System.Data.IsolationLevel;

#pragma warning disable 1998

namespace Rebus.Persistence.SqlServer
{
    /// <summary>
    /// Implementation of <see cref="IDbConnectionProvider"/> that ensures that MARS (multiple active result sets) is enabled on the
    /// given connection string (possibly by enabling it by itself)
    /// </summary>
    public class DbConnectionFactoryProvider : IDbConnectionProvider
    {
        readonly Func<Task<IDbConnection>> _connectionFactory;
        readonly ILog _log;

        /// <summary>
        /// Uses provided SqlConnection factory as constructor for SqlConnection used. Will use <see cref="System.Data.IsolationLevel.ReadCommitted"/> by default on transactions,
        /// unless another isolation level is set with the <see cref="IsolationLevel"/> property
        /// </summary>
        public DbConnectionFactoryProvider(Func<Task<IDbConnection>> connectionFactory, IRebusLoggerFactory rebusLoggerFactory)
        {
            if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));

            _log = rebusLoggerFactory.GetCurrentClassLogger();

            IsolationLevel = IsolationLevel.ReadCommitted;

            _connectionFactory = connectionFactory;
        }

        /// <summary>
        /// Gets a nice ready-to-use database connection with an open transaction
        /// </summary>
        public async Task<IDbConnection> GetConnection()
        {
            try
            {
                return await _connectionFactory();
            }
            catch (Exception exception)
            {
                _log.Warn("An error occurred when invoking the provided connection factory: {0}", exception);
                throw;
            }
        }

        /// <summary>
        /// Gets/sets the isolation level used for transactions
        /// </summary>
        public IsolationLevel IsolationLevel { get; set; }
    }
}