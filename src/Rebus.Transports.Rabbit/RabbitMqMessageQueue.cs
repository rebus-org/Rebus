using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Transactions;
using RabbitMQ.Client;
using System.Linq;

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

        public void Send(string destinationQueueName, TransportMessageToSend message)
        {
            using(var model = connection.CreateModel())
            {
                model.BasicPublish(ExchangeName, destinationQueueName,
                                   GetHeaders(model, message),
                                   GetBody(message));
            }
        }

        public ReceivedTransportMessage ReceiveMessage()
        {
            using (var model = connection.CreateModel())
            {
                var result = model.BasicGet(inputQueueName, false);

                using (new AmbientTxHack(() => model.BasicAck(result.DeliveryTag, false)))
                {
                    return new ReceivedTransportMessage
                               {
                                   Headers = GetHeaders(result.BasicProperties.Headers),
                                   Data = Encoding.GetString(result.Body)
                               };
                }
            }
        }

        class AmbientTxHack : IEnlistmentNotification, IDisposable
        {
            readonly Action commitAction;
            readonly bool isEnlisted;

            public AmbientTxHack(Action commitAction)
            {
                this.commitAction = commitAction;
                isEnlisted = Transaction.Current != null;
            }

            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                AssertEnlisted();
                preparingEnlistment.Prepared();
            }

            public void Commit(Enlistment enlistment)
            {
                AssertEnlisted();
                commitAction();
                enlistment.Done();
            }

            public void Rollback(Enlistment enlistment)
            {
                AssertEnlisted();
                enlistment.Done();
            }

            public void InDoubt(Enlistment enlistment)
            {
                AssertEnlisted();
                enlistment.Done();
            }

            public void Dispose()
            {
                if (!isEnlisted)
                {
                    commitAction();
                }
            }

            void AssertEnlisted()
            {
                if (!isEnlisted)
                {
                    throw new InvalidOperationException("Cannot call ambient TX stuff on non-enlisted TX hack");
                }
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

        IBasicProperties GetHeaders(IModel model, TransportMessageToSend message)
        {
            var props = model.CreateBasicProperties();
            props.Headers = message.Headers != null
                                ? message.Headers.ToDictionary(e => e.Key, e => Encoding.GetBytes(e.Value))
                                : null;
            return props;
        }

        byte[] GetBody(TransportMessageToSend message)
        {
            return Encoding.GetBytes(message.Data);
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
                model.ExchangeDeclare(ExchangeName, ExchangeType.Topic, true);
                model.QueueDeclare(inputQueueName, true, false, false, new Hashtable());
                model.QueueBind(inputQueueName, ExchangeName, inputQueueName);
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
