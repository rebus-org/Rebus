using System;
using System.Data.SqlClient;
using System.Transactions;
using Rebus.Transports.Sql;
using IsolationLevel = System.Data.IsolationLevel;

namespace Rebus.Persistence.SqlServer
{
    /// <summary>
    /// Base class for MS SQL Server storage implementations
    /// </summary>
    public abstract class SqlServerStorage
    {
        protected Func<ConnectionHolder> getConnection;
        protected Action<ConnectionHolder> commitAction;
        protected Action<ConnectionHolder> rollbackAction;
        protected Action<ConnectionHolder> releaseConnection;

        /// <summary>
        /// Constructs the storage with the given connection factory method, assuming that commit/rollback and housekeeping is taken
        /// care of elsewhere
        /// </summary>
        protected SqlServerStorage(Func<ConnectionHolder> connectionFactoryMethod)
        {
            getConnection = connectionFactoryMethod;
            commitAction = h => { };
            rollbackAction = h => { };
            releaseConnection = c => { };
        }

        /// <summary>
        /// Constructs the storage with the given connection string, which will be used to establish the connection. Commit/rollback and
        /// housekeeping is taken care of here
        /// </summary>
        protected SqlServerStorage(string connectionString)
        {
            getConnection = () => CreateConnection(connectionString);
            commitAction = h => h.Commit();
            rollbackAction = h => h.RollBack();
            releaseConnection = c => c.Dispose();
        }

        /// <summary>
        /// Create connection at handle set transaction if necessary
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        protected static ConnectionHolder CreateConnection(string connectionString)
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();

            if (Transaction.Current == null)
            {
                var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
                return ConnectionHolder.ForTransactionalWork(connection, transaction);
            }
            return ConnectionHolder.ForNonTransactionalWork(connection);
        }

    }
}