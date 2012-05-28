using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using Rebus.Logging;
using Rebus.Shared;

namespace Rebus.Transports.Rabbit
{
    public class RabbitMqMessageQueue : ISendMessages, IReceiveMessages, IDisposable
    {
        static ILog log;

        static RabbitMqMessageQueue()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        const string ExchangeName = "Rebus";
        static readonly Encoding Encoding = Encoding.UTF8;

        readonly IConnection connection;

        readonly string inputQueueName;
        readonly string errorQueue;
        readonly IModel model;
        readonly object modelLock = new object();
        readonly Subscription subscription;
        readonly object subscriptionLock = new object();

        public RabbitMqMessageQueue(string connectionString, string inputQueueName, string errorQueue)
        {
            this.inputQueueName = inputQueueName;
            this.errorQueue = errorQueue;

            log.Info("Opening Rabbit connection");
            connection = new ConnectionFactory {Uri = connectionString}.CreateConnection();
            
            log.Debug("Creating model");
            model = connection.CreateModel();

            log.Info("Initializing exchange and input queue");
            var tempModel = connection.CreateModel();

            log.Debug("Ensuring that exchange exists with the name {0}", ExchangeName);
            tempModel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, true);

            CreateLogicalQueue(tempModel, this.inputQueueName);
            CreateLogicalQueue(tempModel, this.errorQueue);

            log.Debug("Opening subscription");
            subscription = new Subscription(model, inputQueueName);
        }

        void CreateLogicalQueue(IModel tempModel, string queueName)
        {
            log.Debug("Declaring queue {0}", queueName);
            tempModel.QueueDeclare(queueName, true, false, false, new Hashtable());

            log.Debug("Binding {0} to {1} (routing key: {2})", queueName, ExchangeName, queueName);
            tempModel.QueueBind(queueName, ExchangeName, queueName);
        }

        public string ErrorQueue
        {
            get { return errorQueue; }
        }

        void WithModel(Action<IModel> handleModel)
        {
            lock(modelLock)
            {
                handleModel(model);
            }
        }

        void WithSubscription(Action<Subscription> handleSubscription)
        {
            lock(subscriptionLock)
            {
                handleSubscription(subscription);
            }
        }

        public ReceivedTransportMessage ReceiveMessage()
        {
            BasicDeliverEventArgs ea = null;
            var gotMessage = false;
            
            WithSubscription(sub => gotMessage = sub.Next(200, out ea));
            
            if (gotMessage)
            {
                using (new AmbientTxHack(() => WithSubscription(sub => sub.Ack(ea)),
                                         () => {},
                                         null))
                {
                    return new ReceivedTransportMessage
                               {
                                   Id = ea.BasicProperties.MessageId,
                                   Headers = GetHeaders(ea.BasicProperties.Headers),
                                   Body = ea.Body,
                               };
                }
            }

            return null;
        }

        public string InputQueue
        {
            get { return inputQueueName; }
        }

        public void Send(string destinationQueueName, TransportMessageToSend message)
        {
            WithModel(m => m.BasicPublish(ExchangeName, destinationQueueName,
                                          GetHeaders(message),
                                          GetBody(message)));
        }

        public RabbitMqMessageQueue PurgeInputQueue()
        {
            WithModel(m => m.QueuePurge(inputQueueName));

            return this;
        }

        public void Dispose()
        {
            log.Info("Disposing Rabbit");
            log.Debug("Closing subscription");
            subscription.Close();

            log.Debug("Disposing model");
            model.Close();
            model.Dispose();

            log.Debug("Disposing connection");
            connection.Close();
            connection.Dispose();
        }

        IBasicProperties GetHeaders(TransportMessageToSend message)
        {
            IBasicProperties props = null;
            WithModel(m =>
                          {
                              props = m.CreateBasicProperties();

                              if (message.Headers != null)
                              {
                                  props.Headers = message.Headers.ToDictionary(e => e.Key,
                                                                               e => Encoding.GetBytes(e.Value));

                                  if (message.Headers.ContainsKey(Headers.ReturnAddress))
                                  {
                                      props.ReplyTo = message.Headers[Headers.ReturnAddress];
                                  }
                              }

                              props.MessageId = Guid.NewGuid().ToString();
                          });
            return props;
        }

        byte[] GetBody(TransportMessageToSend message)
        {
            return message.Body;
        }

        IDictionary<string, string> GetHeaders(IDictionary result)
        {
            if (result == null) return new Dictionary<string, string>();

            return result.Cast<DictionaryEntry>()
                .ToDictionary(e => (string) e.Key, e => Encoding.GetString((byte[]) e.Value));
        }
    }
}