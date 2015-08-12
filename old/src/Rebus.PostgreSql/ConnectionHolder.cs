using System;
using System.Collections.Generic;

using Npgsql;

namespace Rebus.PostgreSql
{
    /// <summary>
    /// Provides an opened and ready-to-use <see cref="NpgsqlConnection"/> for doing stuff in SQL Server.
    /// </summary>
    public class ConnectionHolder : IDisposable
    {
        ConnectionHolder(NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            Connection = connection;
            Transaction = transaction;
        }

        /// <summary>
        /// Gets the current open connection to the database
        /// </summary>
        public NpgsqlConnection Connection { get; private set; }
            
        /// <summary>
        /// Gets the currently ongoing transaction (or null if operating in non-transactional mode)
        /// </summary>
        public NpgsqlTransaction Transaction { get; private set; }

        /// <summary>
        /// Constructs a <see cref="ConnectionHolder"/> instance with the given connection. The connection
        /// will be used for non-transactional work
        /// </summary>
        public static ConnectionHolder ForNonTransactionalWork(NpgsqlConnection connection)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            return new ConnectionHolder(connection, null);
        }

        /// <summary>
        /// Constructs a <see cref="ConnectionHolder"/> instance with the given connection and transaction. The connection
        /// will be used for transactional work
        /// </summary>
        public static ConnectionHolder ForTransactionalWork(NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (transaction == null) throw new ArgumentNullException("transaction");

            return new ConnectionHolder(connection, transaction);
        }

        /// <summary>
        /// Creates a new <see cref="NpgsqlCommand"/>, setting the transaction if necessary
        /// </summary>
        public NpgsqlCommand CreateCommand()
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
        public void Rollback()
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