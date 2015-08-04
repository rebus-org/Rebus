using System;
using Npgsql;

namespace Rebus.PostgreSql
{
    public class PostgresConnection : IDisposable
    {
        readonly NpgsqlConnection _currentConnection;
        readonly NpgsqlTransaction _currentTransaction;

        bool _completed;

        public PostgresConnection(NpgsqlConnection currentConnection, NpgsqlTransaction currentTransaction)
        {
            _currentConnection = currentConnection;
            _currentTransaction = currentTransaction;
        }

        public NpgsqlCommand CreateCommand()
        {
            var command = _currentConnection.CreateCommand();
            command.Transaction = _currentTransaction;
            return command;
        }

        public void Complete()
        {
            _currentTransaction.Commit();
            _completed = true;
        }

        public void Dispose()
        {
            if (!_completed)
            {
                _currentTransaction.Rollback();
            }
            _currentTransaction.Dispose();
            _currentConnection.Dispose();
        }
    }
}