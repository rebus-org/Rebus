using System;
using RabbitMQ.Client;
using Rebus.Logging;

namespace Rebus.RabbitMq
{
    class ConnectionManager : IDisposable
    {
        static ILog _log;

        static ConnectionManager()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly object _activeConnectionLock = new object();
        readonly ConnectionFactory _connectionFactory;

        IConnection _activeConnection;

        public ConnectionManager(string connectionString)
        {
            _log.Info("Initializing RabbitMQ connection manager");

            _connectionFactory = new ConnectionFactory
            {
                Uri = connectionString
            };
        }

        public IConnection GetConnection()
        {
            var connection = _activeConnection;

            if (connection != null) return connection;

            lock (_activeConnectionLock)
            {
                connection = _activeConnection;

                if (connection != null) return connection;

                _log.Info("Creating new RabbitMQ connection");
                connection = _connectionFactory.CreateConnection();

                return connection;
            }
        }

        public void Dispose()
        {
            lock (_activeConnectionLock)
            {
                var connection = _activeConnection;

                if (connection != null)
                {
                    _log.Info("Disposing RabbitMQ connection");

                    connection.Dispose();
                    _activeConnection = null;
                }
            }
        }
    }
}