using System;
using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client;
using Rebus.Logging;

namespace Rebus.RabbitMq
{
    public class ConnectionManager : IDisposable
    {
        static ILog _log;

        static ConnectionManager()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        //readonly List<IConnection> _activeConnections = new List<IConnection>();
        
        readonly object _activeConnectionLock = new object();
        IConnection _activeConnection;


        readonly ConnectionFactory _connectionFactory;

        public ConnectionManager(string connectionString)
        {
            _log.Info("Initializing RabbitMQ connection manager");

            _connectionFactory = new ConnectionFactory
            {
                Uri = connectionString
            };
        }

        public IConnection GetConnection(bool trackConnection = true)
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