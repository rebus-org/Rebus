using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using Rebus.Logging;
using Rebus.Shared;

namespace Rebus.RabbitMQ
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
        readonly IModel subscriptionModel;
        readonly Subscription subscription;
        readonly object subscriptionLock = new object();

        public RabbitMqMessageQueue(string connectionString, string inputQueueName, string errorQueue)
        {
            this.inputQueueName = inputQueueName;
            this.errorQueue = errorQueue;

            log.Info("Opening Rabbit connection");
            connection = new ConnectionFactory { Uri = connectionString }.CreateConnection();

            log.Debug("Creating model");
            subscriptionModel = connection.CreateModel();

            log.Info("Initializing exchange and input queue");
            var tempModel = connection.CreateModel();

            log.Debug("Ensuring that exchange exists with the name {0}", ExchangeName);
            tempModel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, true);

            CreateLogicalQueue(tempModel, this.inputQueueName);
            CreateLogicalQueue(tempModel, this.errorQueue);

            log.Debug("Opening subscription");
            subscription = new Subscription(subscriptionModel, inputQueueName);
        }

        void CreateLogicalQueue(IModel tempModel, string queueName)
        {
            log.Debug("Declaring queue {0}", queueName);
            tempModel.QueueDeclare(queueName, true, false, false, new Hashtable());

            log.Debug("Binding {0} to {1} (routing key: {2})", queueName, ExchangeName, queueName);
            tempModel.QueueBind(queueName, ExchangeName, queueName);
        }

        public string InputQueueAddress
        {
            get { return InputQueue; }
        }

        void WithModel(Action<IModel> handleModel)
        {
            if (Transaction.Current != null)
            {
                if (transactionManager == null)
                {
                    var model = connection.CreateModel();
                    model.TxSelect();
                    transactionManager = new AmbientTxHack(
                        () =>
                        {
                            model.TxCommit();
                            transactionManager = null;
                            ambientModel = null;
                        },
                        () =>
                        {
                            model.TxRollback();
                            transactionManager = null;
                            ambientModel = null;
                        },
                        model);

                    handleModel(model);
                    ambientModel = model;
                }
                else
                {
                    handleModel(ambientModel);
                }
            }
            else
            {
                using (var model = connection.CreateModel())
                {
                    handleModel(model);
                }
            }
        }

        [ThreadStatic]
        static AmbientTxHack transactionManager;

        [ThreadStatic]
        static IModel ambientModel;

        void WithSubscription(Action<Subscription> handleSubscription)
        {
            lock (subscriptionLock)
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
                                         () => { },
                                         null))
                {
                    if (ea == null)
                    {
                        log.Warn("Rabbit Client said there was a message, but the message args was NULL! WTF!?");
                        return null;
                    }

                    var basicProperties = ea.BasicProperties;

                    if (basicProperties == null)
                    {
                        log.Warn("Properties of received message were NULL! WTF!?");
                    }

                    return new ReceivedTransportMessage
                               {
                                   Id = basicProperties != null ? basicProperties.MessageId : "(unknown)",
                                   Headers = basicProperties != null ? GetHeaders(basicProperties.Headers) : new Dictionary<string, string>(),
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
                                          GetHeaders(m, message),
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
            subscriptionModel.Close();
            subscriptionModel.Dispose();

            log.Debug("Disposing connection");
            connection.Close();
            connection.Dispose();
        }

        IBasicProperties GetHeaders(IModel model, TransportMessageToSend message)
        {
            var props = model.CreateBasicProperties();

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
                .ToDictionary(e => (string)e.Key, e => Encoding.GetString((byte[])e.Value));
        }
    }
}