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
        static ILog log;

        static RabbitMqMessageQueue()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        const string ExchangeName = "Rebus";
        static readonly Encoding Encoding = Encoding.UTF8;

        readonly string inputQueueName;
        readonly IConnection connection;

        [ThreadStatic] static ThreadLocalRabbitShit threadLocalRabbitShit;

        readonly Stack<ThreadLocalRabbitShit> stuffToDispose = new Stack<ThreadLocalRabbitShit>();

        public RabbitMqMessageQueue(string connectionString, string inputQueueName)
        {
            this.inputQueueName = inputQueueName;

            connection = new ConnectionFactory { Uri = connectionString }.CreateConnection();

            log.Info("Initializing logical queue '{0}'", inputQueueName);
            using (var localModel = connection.CreateModel())
            {
                log.Debug("Declaring exchange '{0}'", ExchangeName);
                localModel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, true);

                log.Debug("Declaring queue '{0}'", inputQueueName);
                localModel.QueueDeclare(inputQueueName, true, false, false, new Hashtable());

                log.Debug("Binding topic '{0}' to queue '{1}'", inputQueueName, inputQueueName);
                localModel.QueueBind(inputQueueName, ExchangeName, inputQueueName);
            }
        }

        public void Send(string destinationQueueName, TransportMessageToSend message)
        {
            if (Transaction.Current == null)
            {
                log.Debug("No ambient tx detected - sending to '{0}' with local model", destinationQueueName);
                using (var localModel = connection.CreateModel())
                {
                    localModel.BasicPublish(ExchangeName, destinationQueueName,
                                            GetHeaders(localModel, message),
                                            GetBody(message));
                }

                return;
            }

            log.Debug("Ambient tx detected - sending to '{0}' with ambient tx hack", destinationQueueName);

            var hack = GetAmbientTxHack();
            var modelToUse = hack.ModelToUse;
            
            modelToUse.BasicPublish(ExchangeName, destinationQueueName,
                                    GetHeaders(modelToUse, message),
                                    GetBody(message));
        }

        AmbientTxHack GetAmbientTxHack()
        {
            if (threadLocalRabbitShit != null && threadLocalRabbitShit.AmbientTxHack != null)
                return threadLocalRabbitShit.AmbientTxHack;

            if (threadLocalRabbitShit != null)
            {
                log.Debug("We're in a worker thread - will use same model as worker");

                var model = threadLocalRabbitShit.Model;
                threadLocalRabbitShit.AmbientTxHack = new AmbientTxHack(
                    () =>
                        {
                            model.TxCommit();
                            threadLocalRabbitShit.AmbientTxHack = null;
                        },
                    () =>
                        {
                            model.TxRollback();
                            threadLocalRabbitShit.AmbientTxHack = null;
                        }, model, true);

                return threadLocalRabbitShit.AmbientTxHack;
            }
            
            log.Debug("We're outside of a worker thread, but still in a transaction scope");

            if (threadBoundTxHack != null) return threadBoundTxHack;

            var modelToUse = connection.CreateModel();
            modelToUse.TxSelect();

            threadBoundTxHack = new AmbientTxHack(
                () =>
                    {
                        modelToUse.TxCommit();
                        threadBoundTxHack = null;
                    },
                () =>
                    {
                        modelToUse.TxRollback();
                        threadBoundTxHack = null;
                    },
                modelToUse, false);

            return threadBoundTxHack;
        }

        [ThreadStatic] static AmbientTxHack threadBoundTxHack;

        public ReceivedTransportMessage ReceiveMessage()
        {
            if (Transaction.Current == null)
            {
                log.Debug("No ambient tx detected - receiving from '{0}' with local model", inputQueueName);
                using (var localModel = connection.CreateModel())
                {
                    var basicGetResult = localModel.BasicGet(inputQueueName, true);

                    return basicGetResult == null
                               ? null
                               : GetReceivedTransportMessage(basicGetResult.BasicProperties, basicGetResult.Body);
                }
            }

            if (threadLocalRabbitShit == null || threadLocalRabbitShit.Disposed)
            {
                threadLocalRabbitShit = new ThreadLocalRabbitShit();
                log.Debug("Receive called for the first time on {0} - initializing thread bound model",
                          Thread.CurrentThread.Name);

                threadLocalRabbitShit.Model = connection.CreateModel();
                threadLocalRabbitShit.Model.TxSelect();

                log.Debug("Initializing subscription to '{0}'", inputQueueName);
                threadLocalRabbitShit.Subscription = new Subscription(threadLocalRabbitShit.Model, inputQueueName, false);

                stuffToDispose.Push(threadLocalRabbitShit);
            }

            BasicDeliverEventArgs ea;
            if (!threadLocalRabbitShit.Subscription.Next(200, out ea))
                return null;

            if (ea == null)
                return null;

            using (threadLocalRabbitShit.AmbientTxHack = new AmbientTxHack(
                                () =>
                                    {
                                        log.Debug("Ack!");
                                        threadLocalRabbitShit.Subscription.Ack(ea);
                                        log.Debug("Commit!");
                                        threadLocalRabbitShit.Model.TxCommit();
                                        threadLocalRabbitShit.AmbientTxHack = null;
                                    },
                                () =>
                                    {
                                        log.Debug("Rollback!");
                                        threadLocalRabbitShit.Model.TxRollback();
                                        threadLocalRabbitShit.AmbientTxHack = null;
                                    },
                                threadLocalRabbitShit.Model,
                                true))
            {
                return GetReceivedTransportMessage(ea.BasicProperties, ea.Body);
            }
        }

        ReceivedTransportMessage GetReceivedTransportMessage(IBasicProperties basicProperties, byte[] body)
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

        public string InputQueue { get { return inputQueueName; } }

        public string InputQueueAddress { get { return InputQueue; } }

        public RabbitMqMessageQueue PurgeInputQueue()
        {
            using (var localModel = connection.CreateModel())
            {
                localModel.QueuePurge(inputQueueName);
            }

            return this;
        }

        public void Dispose()
        {
            log.Info("Disposing message queue {0}", inputQueueName);

            while (stuffToDispose.Count > 0)
                stuffToDispose.Pop().Dispose();

            log.Debug("Closing connection");
            connection.Close();
            log.Debug("Disposing connection");
            connection.Dispose();
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

        static byte[] GetBody(TransportMessageToSend message)
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

    /*

             static ILog log;

            static RabbitMqMessageQueue()
            {
                RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
            }

            const string ExchangeName = "Rebus";
            static readonly Encoding Encoding = Encoding.UTF8;

            readonly IConnection connection;

            readonly ConcurrentBag<Subscription> threadSubscriptions = new ConcurrentBag<Subscription>();
            readonly ConcurrentBag<IModel> threadModels = new ConcurrentBag<IModel>();

            [ThreadStatic] static Subscription threadSubscription;
            [ThreadStatic] static IModel threadModel;

            readonly string inputQueueName;
            readonly string errorQueue;

            public RabbitMqMessageQueue(string connectionString, string inputQueueName, string errorQueue)
            {
                this.inputQueueName = inputQueueName;
                this.errorQueue = errorQueue;

                log.Info("Opening Rabbit connection");
                connection = new ConnectionFactory { Uri = connectionString }.CreateConnection();

                log.Info("Initializing exchange and input queue");
                using (var tempModel = connection.CreateModel())
                {
                    log.Debug("Ensuring that exchange exists with the name {0}", ExchangeName);
                    tempModel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, true);

                    CreateLogicalQueue(tempModel, this.inputQueueName);
                    CreateLogicalQueue(tempModel, this.errorQueue);
                }
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
                        var model = threadModel ?? CreateNewModel();
                        transactionManager = new AmbientTxHack(
                            () =>
                            {
                                model.TxCommit();
                                transactionManager = null;
                            },
                            () =>
                            {
                                model.TxRollback();
                                transactionManager = null;
                            },
                            model);

                        handleModel(model);
                    }
                    else
                    {
                        handleModel(threadModel);
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

            IModel CreateNewModel()
            {
                var model = connection.CreateModel();
                model.TxSelect();
                return model;
            }

            [ThreadStatic]
            static AmbientTxHack transactionManager;

            void WithSubscription(Action<Subscription> handleSubscription)
            {
                if (threadModel == null)
                {
                    threadModel = CreateNewModel();
                    threadSubscription = new Subscription(threadModel, inputQueueName);

                    threadSubscriptions.Add(threadSubscription);
                    threadModels.Add(threadModel);
                }

                handleSubscription(threadSubscription);
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
                log.Debug("Closing {0} worker subscription(s)", threadSubscriptions.Count);
                foreach (var subscription in threadSubscriptions)
                {
                    subscription.Close();
                }

                log.Debug("Disposing {0} worker model(s)", threadModels.Count);
                foreach (var model in threadModels)
                {
                    model.Close();
                    model.Dispose();
                }

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

 
     */
}
