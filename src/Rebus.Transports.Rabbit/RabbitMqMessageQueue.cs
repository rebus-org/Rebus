using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using System.Linq;
using Rebus.Messages;

namespace Rebus.Transports.Rabbit
{
    public class RabbitMqMessageQueue : ISendMessages, IReceiveMessages, IDisposable
    {
        const string ExchangeName = "Rebus";
        static readonly Encoding Encoding = Encoding.UTF8;

        readonly string inputQueueName;

        IConnection connection;

        public RabbitMqMessageQueue(string inputQueueName)
        {
            this.inputQueueName = inputQueueName;

            OpenConnection();
            EnsureQueueCreated();
        }

        public RabbitMqMessageQueue(string connectionString, string inputQueueName)
        {
            this.inputQueueName = inputQueueName;

            OpenConnection(connectionString);
            EnsureQueueCreated();
        }

        public void Send(string destinationQueueName, TransportMessageToSend message)
        {
            using (var model = connection.CreateModel())
            {
                model.BasicPublish(ExchangeName, destinationQueueName,
                                   GetHeaders(message, model),
                                   GetBody(message));
            }
        }

        public ReceivedTransportMessage ReceiveMessage()
        {
            var model = connection.CreateModel();

            var result = model.BasicGet(inputQueueName, false);
            if (result == null) return null;

            using (new AmbientTxHack(() => model.BasicAck(result.DeliveryTag, false),
                                     () => { },
                                     model))
            {

                var headers = GetHeaders(result.BasicProperties.Headers);

                var receivedTransportMessage = new ReceivedTransportMessage
                                                   {
                                                       Id = result.BasicProperties.MessageId,
                                                       Headers = headers,
                                                       Data = Encoding.GetString(result.Body),
                                                   };
                return receivedTransportMessage;
            }
        }

        void OpenConnection()
        {
            var fac = new ConnectionFactory
                          {
                              UserName = "guest",
                              Password = "guest",
                              HostName = "localhost",
                              Port = AmqpTcpEndpoint.UseDefaultPort
                          };

            connection = fac.CreateConnection();
        }

        void OpenConnection(string connectionString)
        {
            var fac = new ConnectionFactory { Uri = connectionString };
            connection = fac.CreateConnection();
        }

        IBasicProperties GetHeaders(TransportMessageToSend message, IModel model)
        {
            var props = model.CreateBasicProperties();

            if (message.Headers != null)
            {
                props.Headers = message.Headers.ToDictionary(e => e.Key, e => Encoding.GetBytes(e.Value));

                if (message.Headers.ContainsKey(Headers.ReturnAddress))
                {
                    props.ReplyTo = message.Headers[Headers.ReturnAddress];
                }
            }

            props.MessageId = Guid.NewGuid().ToString();

            return props;
        }

        byte[] GetBody(TransportMessageToSend message)
        {
            return Encoding.GetBytes(message.Data);
        }

        static IDictionary<string, string> GetHeaders(IDictionary result)
        {
            if (result == null) return new Dictionary<string, string>();

            return result.Cast<DictionaryEntry>()
                .ToDictionary(e => (string)e.Key, e => Encoding.GetString((byte[])e.Value));
        }

        void EnsureQueueCreated()
        {
            var fac = new ConnectionFactory();
            fac.UserName = "guest";
            fac.Password = "guest";
            fac.HostName = "localhost";
            fac.Port = AmqpTcpEndpoint.UseDefaultPort;

            connection = fac.CreateConnection();

            using (var model = connection.CreateModel())
            {
                model.ExchangeDeclare(ExchangeName, ExchangeType.Topic, true);
                model.QueueDeclare(inputQueueName, true, false, false, new Hashtable());
                model.QueueBind(inputQueueName, ExchangeName, inputQueueName);
            }
        }

        public string InputQueue
        {
            get { return inputQueueName; }
        }

        public void Dispose()
        {
            connection.Close();
            connection.Dispose();
        }

        public RabbitMqMessageQueue PurgeInputQueue()
        {
            using (var model = connection.CreateModel())
            {
                model.QueuePurge(inputQueueName);
            }

            return this;
        }
    }
}
