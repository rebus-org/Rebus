using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        const string ExchangeName = "Rebus";
        static readonly Encoding Encoding = Encoding.UTF8;
        static readonly TimeSpan BackoffTime = TimeSpan.FromMilliseconds(500);
        static ILog log;

        static RabbitMqMessageQueue()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly string inputQueueName;
        readonly IConnection connection;

        bool disposed;

        [ThreadStatic] static IModel threadBoundModel;

        [ThreadStatic] static TxMan threadBoundTxMan;

        [ThreadStatic] static Subscription threadBoundSubscription;

        public RabbitMqMessageQueue(string connectionString, string inputQueueName)
        {
            this.inputQueueName = inputQueueName;

            log.Info("Opening connection to Rabbit queue {0}", inputQueueName);
            connection = new ConnectionFactory { Uri = connectionString }.CreateConnection();

            log.Info("Initializing logical queue '{0}'", inputQueueName);
            using (var model = connection.CreateModel())
            {
                log.Debug("Declaring exchange '{0}'", ExchangeName);
                model.ExchangeDeclare(ExchangeName, ExchangeType.Topic, true);

                log.Debug("Declaring queue '{0}'", inputQueueName);
                model.QueueDeclare(inputQueueName, true, false, false, new Hashtable());

                log.Debug("Binding topic '{0}' to queue '{1}'", inputQueueName, inputQueueName);
                model.QueueBind(inputQueueName, ExchangeName, inputQueueName);
            }
        }

        public void Send(string destinationQueueName, TransportMessageToSend message)
        {
            if (!InAmbientTransaction())
            {
                using (var model = connection.CreateModel())
                {
                    model.BasicPublish(ExchangeName, destinationQueueName,
                                       GetHeaders(model, message),
                                       message.Body);
                }

                return;
            }

            EnsureTxManIsInitialized();

            threadBoundModel.BasicPublish(ExchangeName, destinationQueueName,
                                          GetHeaders(threadBoundModel, message),
                                          message.Body);
        }

        public ReceivedTransportMessage ReceiveMessage()
        {
            if (!InAmbientTransaction())
            {
                using (var localModel = connection.CreateModel())
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

            if (threadBoundSubscription == null)
            {
                threadBoundSubscription = new Subscription(threadBoundModel, inputQueueName, false);
            }

            BasicDeliverEventArgs ea;
            if (!threadBoundSubscription.Next((int) BackoffTime.TotalMilliseconds, out ea))
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
            threadBoundTxMan.OnRollback += () => threadBoundModel.BasicNack(ea.DeliveryTag, false, true);

            return GetReceivedTransportMessage(ea.BasicProperties, ea.Body);
        }

        public string InputQueue { get { return inputQueueName; } }

        public string InputQueueAddress { get { return inputQueueName; } }

        public void Dispose()
        {
            if (disposed) return;

            log.Info("Disposing queue {0}", inputQueueName);

            try
            {
                connection.Close();
                connection.Dispose();
            }
            catch(Exception e)
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
            using (var model = connection.CreateModel())
            {
                log.Warn("Purging queue {0}", inputQueueName);
                model.QueuePurge(inputQueueName);
            }

            return this;
        }

        void EnsureThreadBoundModelIsInitialized()
        {
            if (threadBoundModel != null && threadBoundModel.IsOpen) return;

            threadBoundModel = connection.CreateModel();
            threadBoundModel.TxSelect();
        }

        void EnsureTxManIsInitialized()
        {
            EnsureThreadBoundModelIsInitialized();

            if (threadBoundTxMan != null) return;

            threadBoundTxMan = new TxMan();
            Transaction.Current.EnlistVolatile(threadBoundTxMan, EnlistmentOptions.None);

            threadBoundTxMan.OnCommit += () => threadBoundModel.TxCommit();
            threadBoundTxMan.OnRollback += () => threadBoundModel.TxRollback();
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
    }
}