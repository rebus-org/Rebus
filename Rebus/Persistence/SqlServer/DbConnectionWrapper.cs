using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

#pragma warning disable 1998

namespace Rebus.Persistence.SqlServer
{
    /// <summary>
    /// Wrapper of <see cref="SqlConnection"/> that allows for either handling <see cref="SqlTransaction"/> automatically, or for handling it externally
    /// </summary>
    public class DbConnectionWrapper : IDbConnection
    {
        readonly SqlConnection _connection;
        readonly bool _managedExternally;

        SqlTransaction _currentTransaction;
        bool _disposed;

        /// <summary>
        /// Constructs the wrapper, wrapping the given connection and transaction. It must be indicated with <paramref name="managedExternally"/> whether this wrapper
        /// should commit/rollback the transaction (depending on whether <see cref="Complete"/> is called before <see cref="Dispose()"/>), or if the transaction
        /// is handled outside of the wrapper
        /// </summary>
        public DbConnectionWrapper(SqlConnection connection, SqlTransaction currentTransaction, bool managedExternally)
        {
            _connection = connection;
            _currentTransaction = currentTransaction;
            _managedExternally = managedExternally;
        }

        /// <summary>
        /// Ensures that the wrapper is always disposed
        /// </summary>
        ~DbConnectionWrapper()
        {
            Dispose(false);
        }

        /// <summary>
        /// Creates a ready to used <see cref="SqlCommand"/>
        /// </summary>
        public SqlCommand CreateCommand()
        {
            var sqlCommand = _connection.CreateCommand();
            sqlCommand.Transaction = _currentTransaction;
            return sqlCommand;
        }

        /// <summary>
        /// Gets the names of all the tables in the current database for the current schema
        /// </summary>
        public IEnumerable<string> GetTableNames()
        {
            return _connection.GetTableNames(_currentTransaction);
        }

        /// <summary>
        /// Marks that all work has been successfully done and the <see cref="SqlConnection"/> may have its transaction committed or whatever is natural to do at this time
        /// </summary>
        public async Task Complete()
        {
            if (_managedExternally) return;

            if (_currentTransaction != null)
            {
                using (_currentTransaction)
                {
                    _currentTransaction.Commit();
                    _currentTransaction = null;
                }
            }
        }

        /// <summary>
        /// Finishes the transaction and disposes the connection in order to return it to the connection pool. If the transaction
        /// has not been committed (by calling <see cref="Complete"/>), the transaction will be rolled back.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// If the transaction is handled externally, nothing is done when the wrapper is disposed. Otherwise, the connection
        /// is closed and disposed, and the current transaction is rolled back if <see cref="Complete"/> was not called
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_managedExternally) return;
            if (_disposed) return;

            try
            {
                try
                {
                    if (_currentTransaction != null)
                    {
                        using (_currentTransaction)
                        {
                            _currentTransaction.Rollback();
                            _currentTransaction = null;
                        }
                    }

                    _connection.Close();
                }
                finally
                {
                    _connection.Dispose();
                }
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}