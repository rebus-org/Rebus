using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using RabbitMQ.Client;
using Rebus.Logging;

namespace Rebus.RabbitMq
{
    class ConnectionManager : IDisposable
    {
        readonly object _activeConnectionLock = new object();
        readonly ConnectionFactory[] _connectionFactories;
        readonly ILog _log;

        IConnection _activeConnection;
        int _activeConnectionIndex;
        bool _disposed;

        public ConnectionManager(string connectionString, string inputQueueAddress, IRebusLoggerFactory rebusLoggerFactory)
        {
            if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
            if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));

            _log = rebusLoggerFactory.GetCurrentClassLogger();

            if (inputQueueAddress != null)
            {
                _log.Info("Initializing RabbitMQ connection manager for transport with input queue '{0}'", inputQueueAddress);
            }
            else
            {
                _log.Info("Initializing RabbitMQ connection manager for one-way transport");
            }

            _connectionFactories = connectionString.Split(";,".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(uri => new ConnectionFactory
                {
                    Uri = uri.Trim(),
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(50),
                    ClientProperties = CreateClientProperties(inputQueueAddress)
                })
                .ToArray();

            if (_connectionFactories.Length == 0)
            {
                throw new ArgumentException("Please remember to specify at least one connection string for a RabbitMQ server somewhere. You can also add multiple connection strings separated by ; or , which Rebus will use in failover scenarios");
            }

            if (_connectionFactories.Length > 1)
            {
                _log.Info("RabbitMQ transport has {0} connection strings available", _connectionFactories.Length);
            }
        }

        public IConnection GetConnection()
        {
            var connection = _activeConnection;

            if (connection != null)
            {
                if (connection.IsOpen)
                {
                    return connection;
                }
            }

            lock (_activeConnectionLock)
            {
                connection = _activeConnection;

                if (connection != null)
                {
                    if (connection.IsOpen)
                    {
                        return connection;
                    }

                    _log.Info("Existing connection found to be CLOSED");

                    try
                    {
                        connection.Dispose();
                    }
                    catch { }
                }

                try
                {
                    var indexToUse = _activeConnectionIndex++;
                    _activeConnectionIndex %= _connectionFactories.Length;

                    if (_connectionFactories.Length > 1)
                    {
                        _log.Info("Creating new RabbitMQ connection (from connection with index {0})", indexToUse);
                    }
                    else
                    {
                        _log.Info("Creating new RabbitMQ connection");
                    }

                    _activeConnection = _connectionFactories[indexToUse].CreateConnection();

                    return _activeConnection;
                }
                catch (Exception exception)
                {
                    _log.Warn("Could not establish connection: {0}", exception.Message);
                    Thread.Sleep(500);
                    throw;
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                lock (_activeConnectionLock)
                {
                    var connection = _activeConnection;

                    if (connection != null)
                    {
                        _log.Info("Disposing RabbitMQ connection");

                        // WTF?!?!? RabbitMQ client disposal can THROW!
                        try
                        {
                            connection.Dispose();
                            _activeConnection = null;
                        }
                        catch { }
                    }
                }
            }
            finally
            {
                _disposed = true;
            }
        }

        public void AddClientProperties(Dictionary<string, string> additionalClientProperties)
        {
            foreach (var connectionFactory in _connectionFactories)
            {
                foreach (var kvp in additionalClientProperties)
                {
                    connectionFactory.ClientProperties[kvp.Key] = kvp.Value;
                }
            }
        }

        static IDictionary<string, object> CreateClientProperties(string inputQueueAddress)
        {
            var properties = new Dictionary<string, object>
            {
                {"Type", "Rebus/.NET"},
                {"Machine", Environment.MachineName},
                {"InputQueue", inputQueueAddress ?? "<one-way client>"},
                {"Domain", Environment.UserDomainName},
                {"User", Environment.UserName}
            };

            var currentProcess = Process.GetCurrentProcess();

            properties.Add("ProcessName", currentProcess.ProcessName);
            properties.Add("FileName", currentProcess.MainModule.FileName);

            return properties;
        }
    }
}