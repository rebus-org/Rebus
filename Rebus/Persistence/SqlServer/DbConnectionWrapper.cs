using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Rebus.Exceptions;

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
            try
            {
                return _connection.GetTableNames(_currentTransaction);
            }
            catch (SqlException exception)
            {
                throw new RebusApplicationException(exception, "Could not get table names");
            }
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
                            try
                            {
                                _currentTransaction.Rollback();
                            }
                            catch { }
                            _currentTransaction = null;
                        }
                    }
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