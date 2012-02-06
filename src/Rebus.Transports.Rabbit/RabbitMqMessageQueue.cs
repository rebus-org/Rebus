using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using Rebus.Logging;
using Rebus.Messages;

namespace Rebus.Transports.Rabbit
{
    public class RabbitMqMessageQueue : ISendMessages, IReceiveMessages, IDisposable
    {
        const string ExchangeName = "Rebus";
        static readonly Encoding Encoding = Encoding.UTF8;
        static readonly ILog Log = RebusLoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        readonly IConnection connection;

        readonly string inputQueueName;
        readonly IModel model;
        readonly object modelLock = new object();
        readonly Subscription subscription;
        readonly object subscriptionLock = new object();

        public RabbitMqMessageQueue(string connectionString, string inputQueueName)
        {
            this.inputQueueName = inputQueueName;

            Log.Info("Opening Rabbit connection");
            connection = new ConnectionFactory {Uri = connectionString}.CreateConnection();
            
            Log.Debug("Creating model");
            model = connection.CreateModel();

            Log.Info("Initializing exchange and input queue");
            var tempModel = connection.CreateModel();

            Log.Debug("Ensuring that exchange exists with the name {0}", ExchangeName);
            tempModel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, true);

            Log.Debug("Declaring queue {0}", this.inputQueueName);
            tempModel.QueueDeclare(this.inputQueueName, true, false, false, new Hashtable());
            
            Log.Debug("Binding {0} to {1} (routing key: {2})", this.inputQueueName, ExchangeName, this.inputQueueName);
            tempModel.QueueBind(this.inputQueueName, ExchangeName, this.inputQueueName);

            Log.Debug("Opening subscription");
            subscription = new Subscription(model, inputQueueName);
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
                                   Data = Encoding.GetString(ea.Body),
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
            subscription.Close();
            model.Close();
            model.Dispose();
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
            return Encoding.GetBytes(message.Data);
        }

        IDictionary<string, string> GetHeaders(IDictionary result)
        {
            if (result == null) return new Dictionary<string, string>();

            return result.Cast<DictionaryEntry>()
                .ToDictionary(e => (string) e.Key, e => Encoding.GetString((byte[]) e.Value));
        }
    }
}