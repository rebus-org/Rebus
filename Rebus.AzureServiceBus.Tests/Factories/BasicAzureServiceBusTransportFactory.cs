using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Tests.Contracts.Transports;
using Rebus.Threading.TaskParallelLibrary;
using Rebus.Transport;

namespace Rebus.AzureServiceBus.Tests.Factories
{
    public class BasicAzureServiceBusTransportFactory : ITransportFactory
    {
        public static string ConnectionString
        {
            get
            {
                return ConnectionStringFromFileOrNull(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "asb_connection_string.txt"))
                       ?? ConnectionStringFromEnvironmentVariable("rebus2_asb_connection_string")
                       ?? Throw("Could not find Azure Service Bus connection string!");
            }
        }

        static string Throw(string message)
        {
            throw new ConfigurationErrorsException(message);
        }

        static string ConnectionStringFromEnvironmentVariable(string environmentVariableName)
        {
            var value = Environment.GetEnvironmentVariable(environmentVariableName);

            if (value == null)
            {
                Console.WriteLine("Could not find env variable {0}", environmentVariableName);
                return null;
            }

            Console.WriteLine("Using Azure Service Bus connection string from env variable {0}", environmentVariableName);

            return value;
        }

        static string ConnectionStringFromFileOrNull(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Could not find file {0}", filePath);
                return null;
            }

            Console.WriteLine("Using Azure Service Bus connection string from file {0}", filePath);
            return File.ReadAllText(filePath);
        }

        readonly Dictionary<string, BasicAzureServiceBusTransport> _queuesToDelete = new Dictionary<string, BasicAzureServiceBusTransport>();

        public ITransport CreateOneWayClient()
        {
            return Create(null);
        }

        public ITransport Create(string inputQueueAddress)
        {
            var consoleLoggerFactory = new ConsoleLoggerFactory(false);
            var asyncTaskFactory = new TplAsyncTaskFactory(consoleLoggerFactory);
            if (inputQueueAddress == null)
            {
                var transport = new AzureServiceBusTransport(ConnectionString, null, consoleLoggerFactory, asyncTaskFactory);

                transport.Initialize();

                return transport;
            }

            return _queuesToDelete.GetOrAdd(inputQueueAddress, () =>
            {
                var transport = new BasicAzureServiceBusTransport(ConnectionString, inputQueueAddress, consoleLoggerFactory, asyncTaskFactory);

                transport.PurgeInputQueue();

                transport.Initialize();

                return transport;
            });
        }

        public void CleanUp()
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(ConnectionString);

            _queuesToDelete.Keys.ForEach(queueName =>
            {
                if (!namespaceManager.QueueExists(queueName)) return;

                Console.WriteLine("Deleting ASB queue {0}", queueName);

                namespaceManager.DeleteQueue(queueName);
            });
        }

        public static void DeleteTopic(string topic)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(ConnectionString);

            try
            {
                Console.Write("Deleting topic '{0}' ...", topic);
                namespaceManager.DeleteTopic(topic);
                Console.WriteLine("OK!");
            }
            catch (MessagingEntityNotFoundException)
            {
                Console.WriteLine("OK! (wasn't even there)");
            }
        }
    }
}