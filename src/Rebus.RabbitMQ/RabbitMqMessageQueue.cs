using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Transactions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Shared;

namespace Rebus.RabbitMQ
{
    public class RabbitMqMessageQueue : IMulticastTransport, IDisposable
    {
        const string ExchangeName = "Rebus";
        static readonly Encoding Encoding = Encoding.UTF8;
        static readonly TimeSpan BackoffTime = TimeSpan.FromMilliseconds(500);
        static ILog log;

        readonly ConcurrentBag<string> initializedQueues = new ConcurrentBag<string>();

        static RabbitMqMessageQueue()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly string inputQueueName;
        
        IConnection Connection
        {
            get
            {
                if (currentConnection == null)
                {
                    currentConnection = connectionFactory.CreateConnection();
                    return currentConnection;
                }

                if (!currentConnection.IsOpen)
                {
                    currentConnection.Dispose();
                    currentConnection = connectionFactory.CreateConnection();
                    return currentConnection;
                }

                return currentConnection;
            }
        }

        IConnection currentConnection;

        bool disposed;

        [ThreadStatic]
        static IModel threadBoundModel;

        [ThreadStatic]
        static TxMan threadBoundTxMan;

        [ThreadStatic]
        static Subscription threadBoundSubscription;

        ConnectionFactory connectionFactory;

        public RabbitMqMessageQueue(string connectionString, string inputQueueName)
        {
            this.inputQueueName = inputQueueName;

            log.Info("Opening connection to Rabbit queue {0}", inputQueueName);
            connectionFactory = new ConnectionFactory { Uri = connectionString };

            InitializeLogicalQueue(inputQueueName);
        }

        public void Send(string destinationQueueName, TransportMessageToSend message, ITransactionContext context)
        {
            EnsureInitialized(message, destinationQueueName);

            if (!context.IsTransactional)
            {
                using (var model = Connection.CreateModel())
                {
                    model.BasicPublish(ExchangeName, destinationQueueName,
                                       GetHeaders(model, message),
                                       message.Body);
                }
            }
            else
            {
                var model = GetSenderModel(context);

                model.BasicPublish(ExchangeName, destinationQueueName,
                                   GetHeaders(model, message),
                                   message.Body);
            }
        }

        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            if (!InAmbientTransaction())
            {
                using (var localModel = Connection.CreateModel())
                {
                    var basicGetResult = localModel.BasicGet(inputQueueName, true);

                    if (basicGetResult == null)
                    {
                        Thread.Sleep(BackoffTime);
                        return null;
                    }

                    return GetReceivedTransportMessage(basicGetResult.BasicProperties, basicGetResult.Body);
                }
            }

            EnsureTxManIsInitialized();

            if (threadBoundSubscription == null || !threadBoundSubscription.Model.IsOpen)
            {
                threadBoundSubscription = new Subscription(threadBoundModel, inputQueueName, false);
            }

            BasicDeliverEventArgs ea;
            if (!threadBoundSubscription.Next((int)BackoffTime.TotalMilliseconds, out ea))
            {
                return null;
            }

            // wtf??
            if (ea == null)
            {
                log.Warn("Subscription returned true, but BasicDeliverEventArgs was null!!");
                Thread.Sleep(BackoffTime);
                return null;
            }

            threadBoundTxMan.OnCommit += () => threadBoundSubscription.Ack(ea);
            threadBoundTxMan.AfterRollback += () =>
                {
                    threadBoundModel.BasicNack(ea.DeliveryTag, false, true);
                    threadBoundModel.TxCommit();
                };

            return GetReceivedTransportMessage(ea.BasicProperties, ea.Body);
        }

        IModel GetSenderModel(ITransactionContext context)
        {
            if (context[CurrentModelKey] != null)
                return (IModel)context[CurrentModelKey];

            var model = Connection.CreateModel();
            model.TxSelect();
            context[CurrentModelKey] = model;

            context.DoCommit += model.TxCommit;
            context.DoRollback += model.TxRollback;

            return model;
        }

        const string CurrentModelKey = "current_model";

        public string InputQueue { get { return inputQueueName; } }

        public string InputQueueAddress { get { return inputQueueName; } }

        public void Dispose()
        {
            if (disposed) return;

            log.Info("Disposing queue {0}", inputQueueName);

            try
            {
                Connection.Close();
                Connection.Dispose();
            }
            catch (Exception e)
            {
                log.Error("An error occurred while disposing queue {0}: {1}", inputQueueName, e);
                throw;
            }
            finally
            {
                disposed = true;
            }
        }

        public RabbitMqMessageQueue PurgeInputQueue()
        {
            using (var model = Connection.CreateModel())
            {
                log.Warn("Purging queue {0}", inputQueueName);
                model.QueuePurge(inputQueueName);
            }

            return this;
        }

        void EnsureInitialized(TransportMessageToSend message, string queueName)
        {
            // don't create recipient queue if multicasting
            if (message.Headers.ContainsKey(Headers.Multicast))
            {
                message.Headers.Remove(Headers.Multicast);
                return;
            }

            if (initializedQueues.Contains(queueName)) return;

            lock (initializedQueues)
            {
                if (initializedQueues.Contains(queueName)) return;

                InitializeLogicalQueue(queueName);
                initializedQueues.Add(queueName);
            }
        }

        void InitializeLogicalQueue(string queueName)
        {
            log.Info("Initializing logical queue '{0}'", queueName);
            using (var model = Connection.CreateModel())
            {
                log.Debug("Declaring exchange '{0}'", ExchangeName);
                model.ExchangeDeclare(ExchangeName, ExchangeType.Topic, true);

                log.Debug("Declaring queue '{0}'", queueName);
                model.QueueDeclare(queueName, durable: true,
                                   arguments: new Hashtable(), autoDelete: false, exclusive: false);

                log.Debug("Binding topic '{0}' to queue '{1}'", queueName, queueName);
                model.QueueBind(queueName, ExchangeName, queueName);
            }
        }

        void EnsureThreadBoundModelIsInitialized()
        {
            if (threadBoundModel != null && threadBoundModel.IsOpen) return;

            threadBoundModel = Connection.CreateModel();
            threadBoundModel.TxSelect();
        }

        void EnsureTxManIsInitialized()
        {
            EnsureThreadBoundModelIsInitialized();

            if (threadBoundTxMan != null) return;

            threadBoundTxMan = new TxMan();
            Transaction.Current.EnlistVolatile(threadBoundTxMan, EnlistmentOptions.None);

            threadBoundTxMan.BeforeCommit += () => threadBoundModel.TxCommit();
            threadBoundTxMan.ActualRollback += () => threadBoundModel.TxRollback();
            threadBoundTxMan.Cleanup += () => threadBoundTxMan = null;
        }

        static bool InAmbientTransaction()
        {
            return Transaction.Current != null;
        }

        static IBasicProperties GetHeaders(IModel modelToUse, TransportMessageToSend message)
        {
            var props = modelToUse.CreateBasicProperties();
            
            if (message.Headers != null)
            {
                props.Headers = message.Headers
                    .ToDictionary(e => e.Key,
                                  e => Encoding.GetBytes(e.Value));

                if (message.Headers.ContainsKey(Headers.ReturnAddress))
                {
                    props.ReplyTo = message.Headers[Headers.ReturnAddress];
                }
            }

            props.MessageId = Guid.NewGuid().ToString();
            props.SetPersistent(true);

            return props;
        }

        static ReceivedTransportMessage GetReceivedTransportMessage(IBasicProperties basicProperties, byte[] body)
        {
            return new ReceivedTransportMessage
                {
                    Id = basicProperties != null
                             ? basicProperties.MessageId
                             : "(unknown)",
                    Headers = basicProperties != null
                                  ? GetHeaders(basicProperties.Headers)
                                  : new Dictionary<string, string>(),
                    Body = body,
                };
        }

        static IDictionary<string, string> GetHeaders(IDictionary result)
        {
            if (result == null) return new Dictionary<string, string>();

            return result.Cast<DictionaryEntry>()
                .ToDictionary(e => (string)e.Key, e => Encoding.GetString((byte[])e.Value));
        }

        public bool ManagesSubscriptions { get; private set; }

        public void Subscribe(Type messageType, string inputQueueAddress)
        {
            using (var model = Connection.CreateModel())
            {
                var topic = messageType.FullName;
                log.Info("Subscribing {0} to {1}", inputQueueAddress, topic);
                model.QueueBind(inputQueueAddress, ExchangeName, topic);
            }
        }

        public void Unsubscribe(Type messageType, string inputQueueAddress)
        {
            using (var model = Connection.CreateModel())
            {
                var topic = messageType.FullName;
                log.Info("Unsubscribing {0} from {1}", inputQueueAddress, topic);
                model.QueueUnbind(inputQueueAddress, ExchangeName, topic, new Hashtable());
            }
        }

        public void ManageSubscriptions()
        {
            log.Info("RabbitMQ will manage subscriptions");
            ManagesSubscriptions = true;
        }
    }
}