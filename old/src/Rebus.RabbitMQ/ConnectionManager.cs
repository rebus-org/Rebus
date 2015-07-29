using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using RabbitMQ.Client;
using Rebus.Logging;
using System.Linq;

namespace Rebus.RabbitMQ
{
    internal class ConnectionManager : IDisposable
    {
        static ILog log;

        static readonly DateTime EndpointStartupTime;

        static ConnectionManager()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
            EndpointStartupTime = DateTime.Now;
        }

        readonly string inputQueueName;
        readonly List<ConnectionFactory> connectionFactories;

        IConnection currentConnection;
        int currentConnectionIndex;

        public ConnectionManager(string connectionString, string inputQueueName)
        {
            this.inputQueueName = inputQueueName;

            connectionFactories = connectionString
                .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(CreateConnectionFactory)
                .ToList();

            if (!connectionFactories.Any())
            {
                throw new InvalidOperationException("Cannot create the RabbitMQ connection manager without any connection strings! Please supply" +
                                                    " a connection string (possibly multiple, separated by ,) in order to tell the connection" +
                                                    " manager how to connect to RabbitMQ");
            }
        }

        ConnectionFactory CreateConnectionFactory(string s)
        {
            return
                new ConnectionFactory
                       {
                           Uri = s,
                           ClientProperties =
                               new Dictionary<string, object>
                                   {
                                       {"Machine name", Environment.MachineName},
                                       {"User account", string.Format("{0}\\{1}", Environment.UserDomainName, Environment.UserName)},
                                       {"Startup time", EndpointStartupTime.ToString(CultureInfo.InvariantCulture)},
                                       {"Application command line", Environment.CommandLine},
                                       {"Client", "Rebus endpoint"},
                                       {"Input queue", string.IsNullOrEmpty(inputQueueName) ? "(none)" : inputQueueName},
                                   }
                       };
        }

        public IConnection GetConnection()
        {
            if (currentConnection != null)
            {
                if (currentConnection.IsOpen)
                {
                    return currentConnection;
                }

                ErrorOnConnection(null);
            }

            log.Info("Opening RabbitMQ connection ({0})", currentConnectionIndex);
            var connectionFactoryToUse = connectionFactories[currentConnectionIndex];
            connectionFactoryToUse.ClientProperties["Connected time"] = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            currentConnection = connectionFactoryToUse.CreateConnection();

            return currentConnection;
        }

        public void ErrorOnConnection(Exception exception)
        {
            if (exception != null)
            {
                log.Warn("Rabbit connection {0} failed: {1}", currentConnectionIndex, exception);
            }
            else
            {
                log.Warn("Rabbit connection {0} failed!", currentConnectionIndex);
            }

            try
            {
                if (currentConnection != null)
                {
                    var shutdownReport = string.Join(Environment.NewLine + Environment.NewLine, currentConnection.ShutdownReport.Cast<ShutdownReportEntry>().Select(e => e.Description + ": " + e.Exception));

                    log.Warn(@"Connection failed - close reason: {0} - shutdown report:
{1}",
                             currentConnection.CloseReason, shutdownReport);

                    currentConnection.Dispose();
                }
            }
            catch (Exception)
            {
                // ignore errors while disposing
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
            if (currentConnection != null)
            {
                currentConnection.Dispose();
                currentConnection = null;
            }
        }
    }
}