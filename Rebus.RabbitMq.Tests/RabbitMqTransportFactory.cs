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
            var connectionFactory = new ConnectionFactory { Uri = ConnectionString };

            using (var connection = connectionFactory.CreateConnection())
            using (var model = connection.CreateModel())
            {
                model.QueueDelete(queueName);
            }
        }

        public static void DeleteExchange(string exchangeName)
        {
            var connectionFactory = new ConnectionFactory { Uri = ConnectionString };

            using (var connection = connectionFactory.CreateConnection())
            using (var model = connection.CreateModel())
            {
                model.ExchangeDelete(exchangeName);
            }
        }

        /// <summary>
        /// We check for the existence of the exchange with the name <paramref name="exchangeName"/> by creating another
        /// randomly named exchange and trying to bind from the randomly named one to the one we want to check the existence of.
        /// This causes an exception if the exchange with the name <paramref name="exchangeName"/> does not exists.
        /// </summary>
        public static bool ExchangeExists(string exchangeName)
        {
            var connectionFactory = new ConnectionFactory { Uri = ConnectionString };

            using (var connection = connectionFactory.CreateConnection())
            using (var model = connection.CreateModel())
            {
                try
                {
                    const string nonExistentTopic = "6BE38CB8-089A-4B65-BA86-0801BBC064E9------DELETE-ME";
                    const string fakeExchange = "FEBC2512-CEC6-46EB-A058-37F1A9642B35------DELETE-ME";

                    model.ExchangeDeclare(fakeExchange, ExchangeType.Direct);

                    try
                    {
                        model.ExchangeBind(exchangeName, fakeExchange, nonExistentTopic);
                        model.ExchangeUnbind(exchangeName, fakeExchange, nonExistentTopic);

                        return true;
                    }
                    finally
                    {
                        model.ExchangeDelete(fakeExchange);
                    }
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}