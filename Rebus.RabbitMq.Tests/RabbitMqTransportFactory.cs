using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Tests.Contracts.Transports;
using Rebus.Transport;

namespace Rebus.RabbitMq.Tests
{
    public class RabbitMqTransportFactory : ITransportFactory
    {
        public const string ConnectionString = "amqp://localhost";
        readonly List<IDisposable> _disposables = new List<IDisposable>();
        readonly HashSet<string> _queuesToDelete = new HashSet<string>();

        public ITransport CreateOneWayClient()
        {
            return Create(null);
        }

        public ITransport Create(string inputQueueAddress)
        {
            var transport = new RabbitMqTransport(ConnectionString, inputQueueAddress, new ConsoleLoggerFactory(false));

            _disposables.Add(transport);

            if (inputQueueAddress != null)
            {
                transport.PurgeInputQueue();
            }

            transport.Initialize();

            if (inputQueueAddress != null)
            {
                _queuesToDelete.Add(inputQueueAddress);
            }

            return transport;
        }

        public void CleanUp()
        {
            _disposables.ForEach(d => d.Dispose());
            _disposables.Clear();

            _queuesToDelete.ForEach(DeleteQueue);
            _queuesToDelete.Clear();
        }

        public static void DeleteQueue(string queueName)
        {
            var connectionFactory = new ConnectionFactory {Uri = ConnectionString};

            using (var connection = connectionFactory.CreateConnection())
            using (var model = connection.CreateModel())
            {
                model.QueueDelete(queueName);
            }
        }
    }
}