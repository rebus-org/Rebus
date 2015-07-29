using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using Rebus.Extensions;
using Rebus.Tests.Contracts.Transports;
using Rebus.Transport;

namespace Rebus.RabbitMq.Tests
{
    public class RabbitMqTransportFactory : ITransportFactory
    {
        const string ConnectionString = "amqp://localhost";
        readonly List<IDisposable> _disposables = new List<IDisposable>();
        readonly HashSet<string> _queuesToDelete = new HashSet<string>();

        public ITransport Create(string inputQueueAddress)
        {
            var transport = new RabbitMqTransport(ConnectionString, inputQueueAddress);

            _disposables.Add(transport);

            transport.PurgeInputQueue();

            transport.Initialize();

            _queuesToDelete.Add(inputQueueAddress);

            return transport;
        }

        public void CleanUp()
        {
            _disposables.ForEach(d => d.Dispose());
            _disposables.Clear();

            _queuesToDelete.ForEach(DeleteQueue);
            _queuesToDelete.Clear();
        }

        void DeleteQueue(string queueName)
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