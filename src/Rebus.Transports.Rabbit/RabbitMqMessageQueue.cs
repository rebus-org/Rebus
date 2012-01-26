using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using Rebus.Messages;
using System.Linq;

namespace Rebus.Transports.Rabbit
{
    public class RabbitMqMessageQueue : ISendMessages, IReceiveMessages, IDisposable
    {
        const string Exchange = "rebus";
        readonly string inputQueueName;
        IConnection connection;
        static readonly Encoding Encoding = Encoding.UTF8;

        public RabbitMqMessageQueue(string inputQueueName)
        {
            this.inputQueueName = inputQueueName;

            EnsureQueueCreated();
        }

        public void Send(string destinationQueueName, TransportMessageToSend message)
        {
            var fac = new ConnectionFactory();
            fac.UserName = "guest";
            fac.Password = "guest";
            fac.HostName = "localhost";
            fac.Port = AmqpTcpEndpoint.UseDefaultPort;
            
            connection = fac.CreateConnection();
            
            using(var model = connection.CreateModel())
            {
                model.BasicPublish(Exchange, destinationQueueName,
                                   GetHeaders(model, message),
                                   GetBytes(message));
            }
        }

        static IBasicProperties GetHeaders(IModel model, TransportMessageToSend message)
        {
            var props = model.CreateBasicProperties();
            props.Headers = message.Headers != null
                                ? message.Headers.ToDictionary(e => e.Key, e => Encoding.GetBytes(e.Value))
                                : null;
            return props;
        }

        static byte[] GetBytes(TransportMessageToSend message)
        {
            return Encoding.GetBytes(message.Data);
        }

        public ReceivedTransportMessage ReceiveMessage()
        {
            var fac = new ConnectionFactory();
            fac.UserName = "guest";
            fac.Password = "guest";
            fac.HostName = "localhost";
            fac.Port = AmqpTcpEndpoint.UseDefaultPort;

            connection = fac.CreateConnection();

            using (var model = connection.CreateModel())
            {
                var result = model.BasicGet(inputQueueName, false);
                model.BasicAck(result.DeliveryTag, false);

                return new ReceivedTransportMessage
                           {
                               Headers = GetHeaders(result.BasicProperties.Headers),
                               Data = Encoding.GetString(result.Body)
                           };
            }
        }

        static IDictionary<string,string> GetHeaders(IDictionary result)
        {
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
                model.ExchangeDeclare(Exchange, ExchangeType.Topic, true);
                model.QueueDeclare(inputQueueName, true, false, false, new Hashtable());
                model.QueueBind(inputQueueName, Exchange, inputQueueName);
            }
        }

        string BuildRoutingKey(TransportMessageToSend message)
        {
            return "what is it?";
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
    }
}
