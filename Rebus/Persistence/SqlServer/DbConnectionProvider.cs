using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Rebus.Persistence.SqlServer
{
    public class DbConnectionProvider
    {
        readonly string _connectionString;

        public DbConnectionProvider(string connectionString)
        {
            _connectionString = connectionString;

            IsolationLevel = IsolationLevel.ReadCommitted;
        }

        public async Task<DbConnection> GetConnection()
        {
            var connection = new SqlConnection(_connectionString);
            
            await connection.OpenAsync();

            return new DbConnection(connection, connection.BeginTransaction(IsolationLevel), false);
        }

        public IsolationLevel IsolationLevel { get; set; }
    }

    public class DbConnection : IDisposable
    {
        readonly SqlConnection _connection;
        SqlTransaction _currentTransaction;
        readonly bool _managedExternally;

        public DbConnection(SqlConnection connection, SqlTransaction currentTransaction, bool managedExternally)
        {
            _connection = connection;
            _currentTransaction = currentTransaction;
            _managedExternally = managedExternally;
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
            if (_managedExternally) return;

            if (_currentTransaction != null)
            {
                _currentTransaction.Rollback();
                _currentTransaction = null;
            }
            
            _connection.Dispose();
        }
    }
}