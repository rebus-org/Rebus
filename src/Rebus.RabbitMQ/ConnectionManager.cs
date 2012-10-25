using System;
using System.Collections.Generic;
using System.Threading;
using RabbitMQ.Client;
using Rebus.Logging;
using System.Linq;

namespace Rebus.RabbitMQ
{
    internal class ConnectionManager : IDisposable
    {
        static ILog log;

        static ConnectionManager()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly List<ConnectionFactory> connectionFactories;

        IConnection currentConnection;
        int currentConnectionIndex;

        public ConnectionManager(string connectionString)
        {
            connectionFactories = connectionString
                .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(s => new ConnectionFactory { Uri = s })
                .ToList();

            if (!connectionFactories.Any())
            {
                throw new InvalidOperationException("Cannot create the RabbitMQ connection manager without any connection strings! Please supply" +
                                                    " a connection string (possibly multiple, separated by ,) in order to tell the connection" +
                                                    " manager how to connect to RabbitMQ");
            }
        }

        public IConnection GetConnection()
        {
            if (currentConnection != null)
            {
                if (currentConnection.IsOpen)
                {
                    return currentConnection;
                }

                ErrorOnConnection();
            }

            log.Info("Opening RabbitMQ connection ({0})", currentConnectionIndex);
            currentConnection = connectionFactories[currentConnectionIndex].CreateConnection();

            return currentConnection;
        }

        public void ErrorOnConnection()
        {
            log.Warn("{0}. connection failed! Will dipose it and wait a short while...", currentConnectionIndex);
            try
            {
                if (currentConnection != null)
                {
                    currentConnection.Dispose();
                }
            }
            catch (Exception e)
            {
                log.Error(e, "Error while disposing connection!");
            }
            finally
            {
                currentConnection = null;
                currentConnectionIndex = (currentConnectionIndex + 1) % connectionFactories.Count;
            }

            // to avoid thrashing in case of errors, back out for a short while...
            Thread.Sleep(TimeSpan.FromSeconds(2));
        }

        public void Dispose()
        {
            try
            {
                if (currentConnection != null)
                {
                    currentConnection.Dispose();
                }
            }
            catch (Exception e)
            {
                log.Error(e, "Error while disposing connection!");
            }
        }
    }
}