using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Transport;

#pragma warning disable 1998

namespace Rebus.RabbitMq
{
    /// <summary>
    /// Implementation of <see cref="ITransport"/> that uses RabbitMQ to send/receive messages
    /// </summary>
    public class RabbitMqTransport : ITransport, IDisposable, IInitializable
    {
        const string ExchangeName = "Rebus";
        const string CurrentModelItemsKey = "rabbitmq-current-model";
        const string OutgoingMessagesItemsKey = "rabbitmq-outgoing-messages";

        static readonly Encoding HeaderValueEncoding = Encoding.UTF8;

        readonly ConnectionManager _connectionManager;

        /// <summary>
        /// Constructs the transport with a connection to the RabbitMQ instance specified by the given connection string
        /// </summary>
        public RabbitMqTransport(string connectionString, string inputQueueAddress)
        {
            _connectionManager = new ConnectionManager(connectionString);
            Address = inputQueueAddress;
        }

        public void Initialize()
        {
            CreateQueue(Address);
        }

        public void CreateQueue(string address)
        {
            var connection = _connectionManager.GetConnection();
         
            using (var model = connection.CreateModel())
            {
                model.ExchangeDeclare(ExchangeName, ExchangeType.Topic, true);
                model.QueueDeclare(address, true, false, false, new Dictionary<string, object>
                    {
                        {"x-ha-policy", "all"}
                    });
                model.QueueBind(address, ExchangeName, Address);
            }
        }

        public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            var outgoingMessages = context.GetOrAdd(OutgoingMessagesItemsKey, () =>
            {
                context.OnCommitted(async () => SendOutgoingMessages(context));

                return new ConcurrentQueue<OutgoingMessage>();
            });

            outgoingMessages.Enqueue(new OutgoingMessage(destinationAddress, message));
        }

        public string Address { get; private set; }

        /// <summary>
        /// Deletes all messages from the queue
        /// </summary>
        public void PurgeInputQueue()
        {
            var connection = _connectionManager.GetConnection();

            using (var model = connection.CreateModel())
            {
                try
                {
                    model.QueuePurge(Address);
                }
                catch (OperationInterruptedException exception)
                {
                    var shutdownReason = exception.ShutdownReason;

                    var queueDoesNotExist = shutdownReason != null
                                            && shutdownReason.ReplyCode == 404;

                    if (queueDoesNotExist)
                    {
                        return;
                    }

                    throw;
                }
            }
        }

        public async Task<TransportMessage> Receive(ITransactionContext context)
        {
            var model = GetModel(context);
            var result = model.BasicGet(Address, false);

            if (result == null) return null;

            var deliveryTag = result.DeliveryTag;

            context.OnCompleted(async () =>
            {
                model.BasicAck(deliveryTag, false);
            });

            context.OnAborted(() =>
            {
                model.BasicNack(deliveryTag, false, true);
            });

            var headers = result.BasicProperties.Headers
                .ToDictionary(kvp => kvp.Key, kvp =>
                {
                    var headerValue = kvp.Value;

                    if (headerValue is byte[])
                    {
                        var stringHeaderValue = HeaderValueEncoding.GetString((byte[])headerValue);

                        return stringHeaderValue;
                    }

                    return headerValue.ToString();
                });

            return new TransportMessage(headers, result.Body);
        }

        async Task SendOutgoingMessages(ITransactionContext context)
        {
            var outgoingMessages = context.GetOrThrow<ConcurrentQueue<OutgoingMessage>>(OutgoingMessagesItemsKey);
            var model = GetModel(context);

            foreach (var outgoingMessage in outgoingMessages)
            {
                var destinationAddress = outgoingMessage.DestinationAddress;
                var message = outgoingMessage.TransportMessage;
                var props = model.CreateBasicProperties();
                var headers = message.Headers;
                var timeToBeDelivered = GetTimeToBeReceivedOrNull(message);

                props.Headers = headers
                    .ToDictionary(kvp => kvp.Key, kvp => (object)HeaderValueEncoding.GetBytes(kvp.Value));

                if (timeToBeDelivered.HasValue)
                {
                    props.Expiration = timeToBeDelivered.Value.TotalMilliseconds.ToString("0");
                }

                model.BasicPublish(ExchangeName, destinationAddress, props, message.Body);
            }
        }

        static TimeSpan? GetTimeToBeReceivedOrNull(TransportMessage message)
        {
            var headers = message.Headers;
            TimeSpan? timeToBeReceived = null;
            if (headers.ContainsKey(Headers.TimeToBeReceived))
            {
                var timeToBeReceivedStr = headers[Headers.TimeToBeReceived];
                timeToBeReceived = TimeSpan.Parse(timeToBeReceivedStr);
            }
            return timeToBeReceived;
        }

        IModel GetModel(ITransactionContext context)
        {
            return context.GetOrAdd(CurrentModelItemsKey, () =>
            {
                var connection = _connectionManager.GetConnection();
                var newModel = connection.CreateModel();

                context.OnDisposed(() => newModel.Dispose());

                return newModel;
            });
        }

        class OutgoingMessage
        {
            public OutgoingMessage(string destinationAddress, TransportMessage transportMessage)
            {
                DestinationAddress = destinationAddress;
                TransportMessage = transportMessage;
            }

            public string DestinationAddress { get; private set; }

            public TransportMessage TransportMessage { get; private set; }
        }

        public void Dispose()
        {
            _connectionManager.Dispose();
        }
    }
}