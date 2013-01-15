using System;
using System.Collections;
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
    public class RabbitMqMessageQueue : IMulticastTransport, IDisposable
    {
        public const string ExchangeName = "Rebus";

        static readonly Encoding Encoding = Encoding.UTF8;
        static readonly TimeSpan BackoffTime = TimeSpan.FromMilliseconds(500);
        static ILog log;

        static RabbitMqMessageQueue()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly string inputQueueName;
        readonly bool ensureExchangeIsDeclared;
        readonly ConnectionManager connectionManager;

        readonly object disposalLock = new object();
        volatile bool disposed;

        [ThreadStatic]
        static IModel threadBoundModel;

        [ThreadStatic]
        static Subscription threadBoundSubscription;

        readonly List<Func<Type, string>> eventNameResolvers = new List<Func<Type, string>>();

        public static RabbitMqMessageQueue Sender(string connectionString, bool ensureExchangeIsDeclared)
        {
            return new RabbitMqMessageQueue(connectionString, null, ensureExchangeIsDeclared);
        }

        public RabbitMqMessageQueue(string connectionString, string inputQueueName, bool ensureExchangeIsDeclared = true)
        {
            connectionManager = new ConnectionManager(connectionString, inputQueueName);
            if (inputQueueName == null) return;

            this.inputQueueName = inputQueueName;
            this.ensureExchangeIsDeclared = ensureExchangeIsDeclared;

            InitializeLogicalQueue(inputQueueName);
        }

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

        const string CurrentModelKey = "current_model";

        public string InputQueue { get { return inputQueueName; } }

        public string InputQueueAddress { get { return inputQueueName; } }

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

        public RabbitMqMessageQueue PurgeInputQueue()
        {
            try
            {
                using (var model = GetConnection().CreateModel())
                {
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

        public bool ManagesSubscriptions { get; private set; }

        public void Subscribe(Type messageType, string inputQueueAddress)
        {
            try
            {
                using (var model = GetConnection().CreateModel())
                {
                    var topic = GetEventName(messageType);
                    log.Info("Subscribing {0} to {1}", inputQueueAddress, topic);
                    model.QueueBind(inputQueueAddress, ExchangeName, topic);
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
            try
            {
                using (var model = GetConnection().CreateModel())
                {
                    var topic = GetEventName(messageType);
                    log.Info("Unsubscribing {0} from {1}", inputQueueAddress, topic);
                    model.QueueUnbind(inputQueueAddress, ExchangeName, topic, new Hashtable());
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

        public void ManageSubscriptions()
        {
            log.Info("RabbitMQ will manage subscriptions");
            ManagesSubscriptions = true;
        }

        void InitializeLogicalQueue(string queueName)
        {
            try
            {
                log.Info("Initializing logical queue '{0}'", queueName);
                using (var model = GetConnection().CreateModel())
                {
                    if (ensureExchangeIsDeclared)
                    {
                        log.Debug("Declaring exchange '{0}'", ExchangeName);
                        model.ExchangeDeclare(ExchangeName, ExchangeType.Topic, true);
                    }

                    var arguments = new Hashtable { { "x-ha-policy", "all" } }; //< enable queue mirroring

                    log.Debug("Declaring queue '{0}'", queueName);
                    model.QueueDeclare(queueName, durable: true,
                                       arguments: arguments, autoDelete: false, exclusive: false);

                    log.Debug("Binding topic '{0}' to queue '{1}'", queueName, queueName);
                    model.QueueBind(queueName, ExchangeName, queueName);
                }
            }
            catch (Exception e)
            {
                ErrorOnConnection(e);
                throw;
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

            var newModel  = GetConnection().CreateModel();
            newModel.TxSelect();

            context.DoCommit += newModel.TxCommit;
            context.DoRollback += newModel.TxRollback;

            // ensure any sends within this transaction will use the thread bound model
            context[CurrentModelKey] = newModel;

            // bind it to the thread
            threadBoundModel = newModel;
        }

        static ReceivedTransportMessage GetReceivedTransportMessage(IBasicProperties basicProperties, byte[] body)
        {
            return new ReceivedTransportMessage
                {
                    Id = basicProperties != null
                             ? basicProperties.MessageId
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
                        var milliseconds = (int) timeSpan.TotalMilliseconds;
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
            }

            props.MessageId = Guid.NewGuid().ToString();
            props.SetPersistent(true);

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
            connectionManager.ErrorOnConnection();
        }

        IConnection GetConnection()
        {
            return connectionManager.GetConnection();
        }
    }
}