using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Rebus.Persistence.SqlServer;

namespace Rebus.Transports.Sql
{
    /// <summary>
    /// Provides an opened and ready-to-use <see cref="SqlConnection"/> for doing stuff in SQL Server.
    /// Construct
    /// </summary>
    public class ConnectionHolder : IDisposable
    {
        /// <summary>
        /// Constructs a <see cref="ConnectionHolder"/> instance with the given connection. The connection
        /// will be used for non-transactional work
        /// </summary>
        public static ConnectionHolder ForNonTransactionalWork(SqlConnection connection)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            return new ConnectionHolder(connection, null);
        }

        /// <summary>
        /// Constructs a <see cref="ConnectionHolder"/> instance with the given connection and transaction. The connection
        /// will be used for transactional work
        /// </summary>
        public static ConnectionHolder ForTransactionalWork(SqlConnection connection, SqlTransaction transaction)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (transaction == null) throw new ArgumentNullException("transaction");
            return new ConnectionHolder(connection, transaction);
        }

        ConnectionHolder(SqlConnection connection, SqlTransaction transaction)
        {
            Connection = connection;
            Transaction = transaction;
        }

        /// <summary>
        /// Gets the current open connection to the database
        /// </summary>
        public SqlConnection Connection { get; private set; }
            
        /// <summary>
        /// Gets the currently ongoing transaction (or null if operating in non-transactional mode)
        /// </summary>
        public SqlTransaction Transaction { get; private set; }
            
        /// <summary>
        /// Creates a new <see cref="SqlCommand"/>, setting the transaction if necessary
        /// </summary>
        public SqlCommand CreateCommand()
        {
            var sqlCommand = Connection.CreateCommand();
            
            if (Transaction != null)
            {
                sqlCommand.Transaction = Transaction;
            }
            
            return sqlCommand;
        }

        /// <summary>
        /// Ensures that the ongoing transaction is disposed and the held connection is disposed
        /// </summary>
        public void Dispose()
        {
            if (Transaction != null)
            {
                Transaction.Dispose();
            }
            
            Connection.Dispose();
        }

        /// <summary>
        /// Commits the transaction if one is present
        /// </summary>
        public void Commit()
        {
            if (Transaction == null) return;
            
            Transaction.Commit();
        }

        /// <summary>
        /// Rolls back the transaction is one is present
        /// </summary>
        public void RollBack()
        {
            if (Transaction == null) return;

            Transaction.Rollback();
        }

        /// <summary>
        /// Queries sys.Tables in the current DB
        /// </summary>
        public List<string> GetTableNames()
        {
            return Connection.GetTableNames(Transaction);
        }
    }
}