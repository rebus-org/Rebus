using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Shared;

namespace Rebus.RabbitMQ
{
    /// <summary>
    /// RabbitMQ transport implementation that adds (optional) multicast capabilities to <see cref="IDuplexTransport"/>.
    /// </summary>
    public class RabbitMqMessageQueue : IMulticastTransport, IDisposable, INeedInitializationBeforeStart
    {
        public static class InternalHeaders
        {
            public const string MessageDurability = "rmq-msg-durability";
        }

        static readonly Encoding Encoding = Encoding.UTF8;
        static readonly TimeSpan BackoffTime = TimeSpan.FromMilliseconds(500);
        static ILog log;

        static RabbitMqMessageQueue()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly string inputQueueName;
        readonly ConnectionManager connectionManager;

        readonly object disposalLock = new object();
        volatile bool disposed;

        readonly ConcurrentDictionary<Type, string> subscriptions = new ConcurrentDictionary<Type, string>();
        readonly List<Func<Type, string>> eventNameResolvers = new List<Func<Type, string>>();

        const string CurrentModelKey = "current_model";

        string exchangeName = "Rebus";
        bool ensureExchangeIsDeclared = true;
        bool bindDefaultTopicToInputQueue = true;
        bool autoDeleteInputQueue;
        ushort prefetchCount = 100;
        bool managesSubscriptions;

        [ThreadStatic]
        static IModel threadBoundModel;

        [ThreadStatic]
        static Subscription threadBoundSubscription;

        public static RabbitMqMessageQueue Sender(string connectionString)
        {
            return new RabbitMqMessageQueue(connectionString, null);
        }

        public RabbitMqMessageQueue(string connectionString, string inputQueueName)
        {
            connectionManager = new ConnectionManager(connectionString, inputQueueName);
            if (inputQueueName == null) return;

            this.inputQueueName = inputQueueName;
        }

        bool SenderOnly { get { return string.IsNullOrEmpty(inputQueueName); } }

        public void Send(string destinationQueueName, TransportMessageToSend message, ITransactionContext context)
        {
            try
            {
                if (!context.IsTransactional)
                {
                    using (var model = GetConnection().CreateModel())
                    {
                        var headers = GetHeaders(model, message);
                        model.BasicPublish(ExchangeName, destinationQueueName,
                                           headers,
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
            catch (Exception e)
            {
                ErrorOnConnection(e);
                throw;
            }
        }

        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            try
            {
                // this scenario is only supported for testing purposes - Rebus will always receive messages in a queue transaction
                if (!context.IsTransactional)
                {
                    using (var localModel = GetConnection().CreateModel())
                    {
                        InitializeLogicalQueue(inputQueueName, localModel, autoDeleteInputQueue);

                        var basicGetResult = localModel.BasicGet(inputQueueName, true);

                        if (basicGetResult == null)
                        {
                            Thread.Sleep(BackoffTime);
                            return null;
                        }

                        return GetReceivedTransportMessage(basicGetResult.BasicProperties, basicGetResult.Body);
                    }
                }

                EnsureThreadBoundModelIsInitialized(context);

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
                    return null;
                }

                var subscription = threadBoundSubscription;
                var model = threadBoundModel;

                context.BeforeCommit += () => subscription.Ack(ea);
                context.AfterRollback += () =>
                    {
                        try
                        {
                            model.BasicNack(ea.DeliveryTag, false, true);
                            model.TxCommit();
                        }
                        catch (Exception e)
                        {
                            ErrorOnConnection(e);
                            throw;
                        }
                    };

                return GetReceivedTransportMessage(ea.BasicProperties, ea.Body);
            }
            catch (Exception e)
            {
                ErrorOnConnection(e);
                throw;
            }
        }

        public RabbitMqMessageQueue AddEventNameResolver(Func<Type, string> resolver)
        {
            eventNameResolvers.Add(resolver);
            return this;
        }

        public string InputQueue { get { return inputQueueName; } }

        public string InputQueueAddress { get { return inputQueueName; } }

        public string ExchangeName { get { return exchangeName; } }

        public bool EnsureExchangeIsDeclared { get { return ensureExchangeIsDeclared; } }

        public bool BindDefaultTopicToInputQueue { get { return bindDefaultTopicToInputQueue; } }

        public ushort PrefetchCount { get { return prefetchCount; } }

        public bool ManagesSubscriptions { get { return managesSubscriptions; } }

        public void Dispose()
        {
            if (disposed) return;

            lock (disposalLock)
            {
                if (disposed) return;

                log.Info("Disposing queue {0}", inputQueueName);

                try
                {
                    connectionManager.Dispose();
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
        }

        /// <summary>
        /// Deletes all messages in the input queue.
        /// </summary>
        public RabbitMqMessageQueue PurgeInputQueue()
        {
            try
            {
                using (var model = GetConnection().CreateModel())
                {
                    InitializeLogicalQueue(inputQueueName, model, autoDeleteInputQueue);
                    log.Warn("Purging queue {0}", inputQueueName);
                    model.QueuePurge(inputQueueName);
                }

                return this;
            }
            catch (Exception e)
            {
                ErrorOnConnection(e);
                throw;
            }
        }

        public void Subscribe(Type messageType, string inputQueueAddress)
        {
            if (autoDeleteInputQueue)
            {
                subscriptions.TryAdd(messageType, "");

                var model = threadBoundModel;
                if (model != null)
                {
                    var topic = GetEventName(messageType);
                    log.Info("Subscribing {0} to {1}", InputQueueAddress, topic);
                    model.QueueBind(InputQueueAddress, ExchangeName, topic);
                }
                return;
            }

            try
            {
                using (var model = GetConnection().CreateModel())
                {
                    var topic = GetEventName(messageType);
                    log.Info("Subscribing {0} to {1}", InputQueueAddress, topic);
                    model.QueueBind(InputQueueAddress, ExchangeName, topic);
                }
            }
            catch (Exception e)
            {
                ErrorOnConnection(e);
                throw;
            }
        }

        public void Unsubscribe(Type messageType, string inputQueueAddress)
        {
            if (autoDeleteInputQueue)
            {
                string dummy;
                subscriptions.TryRemove(messageType, out dummy);

                var model = threadBoundModel;
                if (model != null)
                {
                    var topic = GetEventName(messageType);
                    log.Info("Unsubscribing {0} from {1}", InputQueueAddress, topic);
                    model.QueueUnbind(InputQueueAddress, ExchangeName, topic, new Hashtable());
                }
                return;
            }

            try
            {
                using (var model = GetConnection().CreateModel())
                {
                    var topic = GetEventName(messageType);
                    log.Info("Unsubscribing {0} from {1}", InputQueueAddress, topic);
                    model.QueueUnbind(InputQueueAddress, ExchangeName, topic, new Hashtable());
                }
            }
            catch (Exception e)
            {
                ErrorOnConnection(e);
                throw;
            }
        }

        public string GetEventName(Type messageType)
        {
            foreach (var tryResolve in eventNameResolvers)
            {
                var eventName = tryResolve(messageType);

                if (eventName != null)
                    return eventName;
            }

            return GetPrettyTypeName(messageType);
        }

        public RabbitMqMessageQueue ManageSubscriptions()
        {
            log.Info("RabbitMQ will manage subscriptions");
            managesSubscriptions = true;
            return this;
        }

        public RabbitMqMessageQueue DoNotBindDefaultTopicToInputQueue()
        {
            log.Info("Will not bind default topic {0} to input queue {1}", inputQueueName, inputQueueName);
            bindDefaultTopicToInputQueue = false;
            return this;
        }

        public RabbitMqMessageQueue DoNotDeclareExchange()
        {
            log.Info("Will not automatically (re)declare exchange");
            ensureExchangeIsDeclared = false;
            return this;
        }

        public RabbitMqMessageQueue UseExchange(string exchangeNameToUse)
        {
            log.Info("Will use exchanged named {0}", exchangeNameToUse);
            exchangeName = exchangeNameToUse;
            return this;
        }

        public RabbitMqMessageQueue Prefetch(ushort prefetchCountToSet)
        {
            log.Info("Will set prefetch count to {0} on new connections", prefetchCountToSet);
            prefetchCount = prefetchCountToSet;
            return this;
        }

        public RabbitMqMessageQueue AutoDeleteInputQueue()
        {
            log.Info("Will set the autodelete flag on input queue");
            autoDeleteInputQueue = true;
            return this;
        }

        IModel GetSenderModel(ITransactionContext context)
        {
            if (context[CurrentModelKey] != null)
                return (IModel)context[CurrentModelKey];

            var model = GetConnection().CreateModel();
            model.TxSelect();
            context[CurrentModelKey] = model;

            context.DoCommit += model.TxCommit;
            context.DoRollback += model.TxRollback;

            return model;
        }

        void EstablishSubscriptions(IModel model)
        {
            if (model == null) return;

            foreach (var subscription in subscriptions.Keys)
            {
                var topic = GetEventName(subscription);
                log.Info("Subscribing {0} to {1}", InputQueueAddress, topic);
                model.QueueBind(InputQueueAddress, ExchangeName, topic);
            }
        }

        static string GetPrettyTypeName(Type messageType)
        {
            if (messageType.IsGenericType)
            {
                var genericTypeDefinition = messageType.GetGenericTypeDefinition();
                var genericArguments = messageType.GetGenericArguments();

                var fullName = genericTypeDefinition.FullName;
                var substring = fullName.Substring(0, fullName.IndexOf("`"));

                var builder = new StringBuilder();
                builder.Append(substring);
                builder.Append("<");
                var first = true;
                foreach (var genericArgument in genericArguments)
                {
                    if (!first) builder.Append(", ");
                    builder.Append(GetPrettyTypeName(genericArgument));
                    first = false;
                }
                builder.Append(">");
                return builder.ToString();
            }
            return messageType.FullName;
        }

        void InitializeLogicalQueue(string queueName, IModel model, bool autoDelete = false)
        {
            log.Info("Initializing logical queue '{0}'", queueName);

            var arguments = new Hashtable { { "x-ha-policy", "all" } }; //< enable queue mirroring

            log.Debug("Declaring queue '{0}'", queueName);
            model.QueueDeclare(queueName, durable: true,
                               arguments: arguments,
                               autoDelete: autoDelete,
                               exclusive: false);

            if (ensureExchangeIsDeclared)
            {
                log.Debug("Declaring exchange '{0}'", ExchangeName);
                model.ExchangeDeclare(ExchangeName, ExchangeType.Topic, true);
            }

            if (bindDefaultTopicToInputQueue)
            {
                log.Debug("Binding topic '{0}' to queue '{1}'", queueName, queueName);
                model.QueueBind(queueName, ExchangeName, queueName);
            }
        }

        internal void CreateQueue(string errorQueueName)
        {
            WithConnection(model => InitializeLogicalQueue(errorQueueName, model));
        }

        public void Initialize()
        {
            if (SenderOnly) return;

            using (var model = GetConnection().CreateModel())
            {
                InitializeLogicalQueue(inputQueueName, model, autoDeleteInputQueue);
            }
        }

        void EnsureThreadBoundModelIsInitialized(ITransactionContext context)
        {
            if (threadBoundModel != null && threadBoundModel.IsOpen)
            {
                if (context[CurrentModelKey] == null)
                {
                    var model = threadBoundModel;
                    context[CurrentModelKey] = model;
                    context.DoCommit += model.TxCommit;
                    context.DoRollback += model.TxRollback;
                }
                return;
            }

            var newModel = GetConnection().CreateModel();
            newModel.TxSelect();
            newModel.BasicQos(0, prefetchCount, false);

            context.DoCommit += newModel.TxCommit;
            context.DoRollback += newModel.TxRollback;

            // ensure any sends within this transaction will use the thread bound model
            context[CurrentModelKey] = newModel;

            // bind it to the thread
            threadBoundModel = newModel;

            InitializeLogicalQueue(inputQueueName, threadBoundModel, autoDeleteInputQueue);

            EstablishSubscriptions(threadBoundModel);
        }

        static ReceivedTransportMessage GetReceivedTransportMessage(IBasicProperties basicProperties, byte[] body)
        {
            return new ReceivedTransportMessage
                {
                    Id = basicProperties != null
                             ? basicProperties.MessageId ?? "(null)"
                             : "(unknown)",
                    Headers = basicProperties != null
                                  ? GetHeaders(basicProperties)
                                  : new Dictionary<string, object>(),
                    Body = body,
                };
        }

        static IDictionary<string, object> GetHeaders(IBasicProperties basicProperties)
        {
            var headers = basicProperties.Headers;

            if (headers == null) return new Dictionary<string, object>();

            return headers.ToDictionary(de => (string)de.Key, de => PossiblyDecode(de.Value));
        }

        static IBasicProperties GetHeaders(IModel modelToUse, TransportMessageToSend message)
        {
            var props = modelToUse.CreateBasicProperties();

            var persistentMessage = true;

            if (message.Headers != null)
            {
                props.Headers = message.Headers
                    .ToHashtable(kvp => kvp.Key, kvp => PossiblyEncode(kvp.Value));

                if (message.Headers.ContainsKey(Headers.ReturnAddress))
                {
                    props.ReplyTo = (string)message.Headers[Headers.ReturnAddress];
                }

                if (message.Headers.ContainsKey(Headers.TimeToBeReceived))
                {
                    var timeToBeReceived = message.Headers[Headers.TimeToBeReceived] as string;

                    if (timeToBeReceived == null)
                    {
                        throw new ArgumentException(
                            string.Format(
                                "Message header contains the {0} header, but the value is {1} and not a string as expected!",
                                Headers.TimeToBeReceived, message.Headers[Headers.TimeToBeReceived]));
                    }

                    try
                    {
                        var timeSpan = TimeSpan.Parse(timeToBeReceived);
                        var milliseconds = (int)timeSpan.TotalMilliseconds;
                        if (milliseconds <= 0)
                        {
                            throw new ArgumentException(
                                string.Format(
                                    "Cannot set TTL message expiration to {0} milliseconds! Please specify a positive value!",
                                    milliseconds));
                        }
                        props.Expiration = milliseconds.ToString();
                    }
                    catch (Exception e)
                    {
                        throw new FormatException(string.Format(
                            "Could not set TTL message expiration on message - apparently, '{0}' is not a valid TTL TimeSpan",
                            timeToBeReceived), e);
                    }
                }

                if (message.Headers.ContainsKey(InternalHeaders.MessageDurability))
                {
                    var durableMessages = (message.Headers[InternalHeaders.MessageDurability] ?? "").ToString();

                    bool result;
                    if (bool.TryParse(durableMessages, out result))
                    {
                        persistentMessage = result;
                    }
                    else
                    {
                        throw new ArgumentException(
                            string.Format("Could not parse the value '{0}' from the '{1}' header into a proper bool",
                                          durableMessages, InternalHeaders.MessageDurability));
                    }
                }
            }

            props.MessageId = Guid.NewGuid().ToString();
            props.SetPersistent(persistentMessage);

            return props;
        }

        static object PossiblyEncode(object value)
        {
            if (!(value is string)) return value;

            return Encoding.GetBytes((string)value);
        }

        static object PossiblyDecode(object value)
        {
            if (!(value is byte[])) return value;

            return Encoding.GetString((byte[])value);
        }

        void ErrorOnConnection(Exception exception)
        {
            connectionManager.ErrorOnConnection(exception);
        }

        IConnection GetConnection()
        {
            return connectionManager.GetConnection();
        }

        void WithConnection(Action<IModel> action)
        {
            using(var model = GetConnection().CreateModel())
                action(model);
        }
    }
}