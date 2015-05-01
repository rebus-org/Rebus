using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Rebus.Persistence.SqlServer
{
    public class DbConnectionWrapper : IDbConnection
    {
        readonly SqlConnection _connection;
        readonly bool _managedExternally;

        SqlTransaction _currentTransaction;
        bool _disposed;

        public DbConnectionWrapper(SqlConnection connection, SqlTransaction currentTransaction, bool managedExternally)
        {
            _connection = connection;
            _currentTransaction = currentTransaction;
            _managedExternally = managedExternally;
        }

        ~DbConnectionWrapper()
        {
            Dispose(false);
        }

        public SqlCommand CreateCommand()
        {
            var sqlCommand = _connection.CreateCommand();
            sqlCommand.Transaction = _currentTransaction;
            return sqlCommand;
        }

        public IEnumerable<string> GetTableNames()
        {
            return _connection.GetTableNames(_currentTransaction);
        }

        public async Task Complete()
        {
            if (_managedExternally) return;

            if (_currentTransaction != null)
            {
                _currentTransaction.Commit();
                _currentTransaction = null;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_managedExternally) return;
            if (_disposed) return;

            try
            {
                if (_currentTransaction != null)
                {
                    _currentTransaction.Rollback();
                    _currentTransaction = null;
                }

                using(_connection)
                {
                    _connection.Close();
                }
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}