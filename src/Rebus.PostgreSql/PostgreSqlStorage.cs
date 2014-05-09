using System;
using System.Transactions;

using Npgsql;

using IsolationLevel = System.Data.IsolationLevel;

namespace Rebus.PostgreSql
{
    /// <summary>
    /// Base class for PostgreSQL storage implementations.
    /// </summary>
    public abstract class PostgreSqlStorage
    {
        protected Func<ConnectionHolder> getConnection;
        protected Action<ConnectionHolder> commitAction;
        protected Action<ConnectionHolder> rollbackAction;
        protected Action<ConnectionHolder> releaseConnection;

        protected PostgreSqlStorage(Func<ConnectionHolder> connectionFactoryMethod)
        {
            getConnection = connectionFactoryMethod;
            commitAction = h => { };
            rollbackAction = h => { };
            releaseConnection = c => { };
        }

        protected PostgreSqlStorage(string connectionString)
        {
            getConnection = () => CreateConnection(connectionString);
            commitAction = h => h.Commit();
            rollbackAction = h => h.Rollback();
            releaseConnection = h => h.Dispose();
        }

        protected static ConnectionHolder CreateConnection(string connectionString)
        {
            var connection = new NpgsqlConnection(connectionString);

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