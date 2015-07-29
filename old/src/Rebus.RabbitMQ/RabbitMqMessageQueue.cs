using System;
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
    using System.Linq;

    /// <summary>
    /// RabbitMQ transport implementation that adds (optional) multicast capabilities to <see cref="IDuplexTransport"/>.
    /// </summary>
    public class RabbitMqMessageQueue : IMulticastTransport, IDisposable, INeedInitializationBeforeStart
    {
        /// <summary>
        /// Headers that affect how Rabbit actually transports the messages
        /// </summary>
        public static class InternalHeaders
        {
            /// <summary>
            /// Affects whether Rabbit messages are sent with the "persistent" setting on or off
            /// </summary>
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
        const int MaxMessageBodySize = 44739489; //< this is 32768 KB of '*' chars in a byte array, wrapped in a message

        string exchangeName = "Rebus";
        bool ensureExchangeIsDeclared = true;
        bool bindDefaultTopicToInputQueue = true;
        bool autoDeleteInputQueue;
        ushort prefetchCount = 100;
        bool managesSubscriptions;
        string inputExchangeAddress;
        bool ensureInputExchangeIsDeclared = true;

        [ThreadStatic]
        static IModel threadBoundModel;

        [ThreadStatic]
        static Subscription threadBoundSubscription;

        /// <summary>
        /// Constructs the transport with the given connection string in "send-only" mode. This means that
        /// the transport can only send messages (note: and publish if Rabbit manages the subscriptions)
        /// </summary>
        public static RabbitMqMessageQueue Sender(string connectionString)
        {
            return new RabbitMqMessageQueue(connectionString, null);
        }

        /// <summary>
        /// Constructs the transport with the given connection string and input queue name. Depending on
        /// further configuration, exchange, input queue, and bindings may/may not be initialized when the
        /// transport is used.
        /// </summary>
        public RabbitMqMessageQueue(string connectionString, string inputQueueName)
        {
            string exchange;
            GetExchangeAndRoutingKeyFor(inputQueueName, out exchange, out inputQueueName);

            connectionManager = new ConnectionManager(connectionString, inputQueueName);
            if (inputQueueName == null) return;

            this.inputQueueName = inputQueueName;
        }

        /// <summary>
        /// Initializes the transport by open a connection, setting up input queue etc
        /// </summary>
        public void Initialize()
        {
            if (SenderOnly) return;

            using (var model = GetConnection().CreateModel())
            {
                InitializeLogicalQueue(inputQueueName, model, false, autoDeleteInputQueue);
            }
        }

        bool SenderOnly { get { return string.IsNullOrEmpty(inputQueueName); } }

        void GetExchangeAndRoutingKeyFor(string destinationAddress, out string exchange, out string routingKey)
        {
            var address = (destinationAddress ?? "");

            if (!address.Contains('@'))
            {
                // We're routing (directly) towards a queue, which means:
                //  - Classic Routing => use configured ExchangeName.
                //  - OneExchangePerTypr => use empty-string as exchange name.
                exchange = ExchangeName ?? "";
                routingKey = address;
            }
            else if (address.StartsWith("@"))
            {
                // We're routing (directly) towards an exchange, so use empty-string routing key.
                exchange = address.TrimStart('@');
                routingKey = "";
            }
            else
            {
                // We're are routing towards an exchange, but with an specific routing key.
                var addressParts = address.Split('@');
                exchange = addressParts[1];
                routingKey = addressParts[0];
            }
        }

        /// <summary>
        /// Sends the specified message to the queue specified by <see cref="destination"/>. Please
        /// note that <see cref="destination"/> is not actually a queue, it's a destination address.
        /// Which may correspond to a topic address (routingKey@exchange), or to a single queue (queue),
        /// or even simply an exchange (@exchange).
        /// </summary>
        public void Send(string destination, TransportMessageToSend message, ITransactionContext context)
        {
            var messageBodyLength = message.Body.Length;

            if (messageBodyLength > MaxMessageBodySize)
            {
                throw new InvalidOperationException(string.Format("Message body length of {0} bytes exceeds the maximum message body length of {1} bytes",
                    messageBodyLength, MaxMessageBodySize));
            }

            try
            {
                string exchange, routingKey;

                // If we are publishing with one-exchange-per-type strategy
                // passed destination should be pressumed to be an exchange.
                if (message.Headers.ContainsKey(Headers.Multicast)
                    && !message.Headers.ContainsKey(Headers.Bounced)
                    && UsingOneExchangePerMessageTypeRouting
                    && !destination.Contains('@'))
                {
                    destination = "@" + destination;
                }

                GetExchangeAndRoutingKeyFor(destination, out exchange, out routingKey);

                if (!context.IsTransactional)
                {
                    using (var model = GetConnection().CreateModel())
                    {
                        var headers = GetHeaders(model, message);
                        // If we are publishing in OneExchangePerType, we have to ensure that
                        // the exchange exists, if not, the connection get closed abruptly
                        if (message.Headers.ContainsKey(Headers.Multicast)
                            && !message.Headers.ContainsKey(Headers.Bounced)
                            && UsingOneExchangePerMessageTypeRouting)
                        {
                            log.Debug("Declaring fanout exchange for: {0}", exchange);
                            model.ExchangeDeclare(exchange, "fanout", true, false, null);
                        }
                        model.BasicPublish(exchange, routingKey, headers, message.Body);
                    }
                }
                else
                {
                    var model = GetSenderModel(context);
                    if (message.Headers.ContainsKey(Headers.Multicast)
                            && !message.Headers.ContainsKey(Headers.Bounced)
                            && UsingOneExchangePerMessageTypeRouting)
                    {
                        log.Debug("Declaring fanout exchange for: {0}", exchange);
                        model.ExchangeDeclare(exchange, "fanout", true, false, null);
                    }
                    model.BasicPublish(exchange, routingKey, GetHeaders(model, message), message.Body);
                }
            }
            catch (Exception e)
            {
                ErrorOnConnection(e);
                throw;
            }
        }

        bool UsingOneExchangePerMessageTypeRouting
        {
            get { return ExchangeName == null; }
        }

        /// <summary>
        /// Attempts to receive a message, returning null if no message was available
        /// </summary>
        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            try
            {
                // this scenario is only supported for testing purposes - Rebus will always receive messages in a queue transaction
                if (!context.IsTransactional)
                {
                    using (var localModel = GetConnection().CreateModel())
                    {
                        InitializeLogicalQueue(inputQueueName, localModel, false, autoDeleteInputQueue);

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
                    log.Warn("End-of-stream detected - will reset subscription and underlying model");
                    
                    // just "forget" this one
                    threadBoundSubscription = null;

                    // if we have this one, make sure to dispose it
                    if (threadBoundModel != null)
                    {
                        threadBoundModel.Dispose();
                        threadBoundModel = null;
                    }

                    // if the connection manager's connection survived, we should be good the next time we 
                    // EnsureThreadBoundModelIsInitialized ... otherwise, the initialization will throw,
                    // which will cause the connection manager to throw out the connection and attempt
                    // to re-connect
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

        /// <summary>
        /// Adds a function to the list of functions that will be asked to resolve the topic for a given type
        /// </summary>
        public RabbitMqMessageQueue AddEventNameResolver(Func<Type, string> resolver)
        {
            eventNameResolvers.Add(resolver);
            return this;
        }

        /// <summary>
        /// Gets name of the input queue. Returns the same as <see cref="InputQueueAddress"/>
        /// because all queues are global with RabbitMQ.
        /// </summary>
        public string InputQueue
        {
            get { return inputQueueName; }
        }

        /// <summary>
        /// Gets the globally accessible name of the input queue. Returns the same as <see cref="InputQueue"/>
        /// because all queues are global with RabbitMQ.
        /// </summary>
        public string InputQueueAddress
        {
            get { return inputExchangeAddress ?? inputQueueName; }
        }

        /// <summary>
        /// Gets a value indicating whether <see cref="InputQueueAddress"/> is an exchange address.
        /// </summary>
        /// <value>
        /// <c>true</c> if [input queue address is exchange]; otherwise, <c>false</c>.
        /// </value>
        public bool InputQueueAddressIsExchange
        {
            get { return inputExchangeAddress != null; }
        }

        /// <summary>
        /// Gets the name of the exchange that messages are published to
        /// </summary>
        public string ExchangeName
        {
            get { return exchangeName; }
        }

        /// <summary>
        /// Indicates whether subscriptions are managed by RabbitMQ. This means that a subscription corresponds to
        /// a binding made from a topic with a .NET type name in RabbitMQ, and a <see cref="IBus.Publish{TEvent}"/>
        /// correponds to publishing the given message with the type name as topic.
        /// </summary>
        public bool ManagesSubscriptions
        {
            get { return managesSubscriptions; }
        }

        /// <summary>
        /// Disposes the connection manager, thereby closing & disposing Rabbit connections
        /// </summary>
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
                    InitializeLogicalQueue(inputQueueName, model, false, autoDeleteInputQueue);
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

        /// <summary>
        /// When managing subscriptions, the transport will be called when subscribing and unsubscribing.
        /// This will result in binding and unbinding, respectively, to the topic for the given <see cref="eventType"/>
        /// </summary>
        public void Subscribe(Type eventType, string inputQueueAddress)
        {
            if (SenderOnly)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Attempted to subscribe to {0}, but the transport is configured to work in one-way client mode which implies that messages can only be sent/published and not received",
                        eventType));
            }

            EnsureInputQueueInitialized(inputQueueAddress);

            if (autoDeleteInputQueue)
            {
                subscriptions.TryAdd(eventType, "");
                EstablishSubscriptions(threadBoundModel);
                
                // if we have the connection now, make sure the subscription is established on that
                var model = threadBoundModel;
                if (model != null)
                {
                    EstablishSubscription(model, eventType);
                }
                else
                {
                    // in case we have already established a connection on another thread, we must ensure the binding happens
                    // - if the connection has not been established, this will end up as a temporarily visible queue with
                    // one binding, which disappears immediately
                    WithConnection(m => EstablishSubscription(m, eventType));
                }
                return;
            }

            try
            {
                using (var model = GetConnection().CreateModel())
                {
                    EstablishSubscription(model, eventType);
                }
            }
            catch (Exception e)
            {
                ErrorOnConnection(e);
                throw;
            }
        }

        /// <summary>
        /// When managing subscriptions, the transport will be called when subscribing and ubsubscribing.
        /// This will result in binding and unbinding, respectively, to the topic for the given <see cref="messageType"/>
        /// </summary>
        public void Unsubscribe(Type messageType, string inputQueueAddress)
        {
            if (SenderOnly)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Attempted to subscribe to {0}, but the transport is configured to work in one-way client mode which implies that messages can only be sent/published and not received",
                        messageType));
            }

            EnsureInputQueueInitialized(inputQueueAddress);

            if (autoDeleteInputQueue)
            {
                string dummy;
                subscriptions.TryRemove(messageType, out dummy);

                var model = threadBoundModel;
                if (model == null) return;

                RemoveSubscription(model, messageType);
                return;
            }

            try
            {
                using (var model = GetConnection().CreateModel())
                {
                    RemoveSubscription(model, messageType);
                }
            }
            catch (Exception e)
            {
                ErrorOnConnection(e);
                throw;
            }
        }

        /// <summary>
        /// Gets the event name for the given message type. This will be used as the topic when the given
        /// message type is published
        /// </summary>
        public string GetEventName(Type messageType)
        {
            foreach (var tryResolve in eventNameResolvers)
            {
                var eventName = tryResolve(messageType);

                if (eventName != null)
                {
                    return eventName;
                }
            }

            return GetPrettyTypeName(messageType);
        }

        /// <summary>
        /// Instructs the transport that it should function as a subscription storage and delegate multicast
        /// operations to the transport layer.
        /// </summary>
        public RabbitMqMessageQueue ManageSubscriptions()
        {
            log.Info("RabbitMQ will manage subscriptions");
            managesSubscriptions = true;
            return this;
        }

        /// <summary>
        /// Configures the transport to NOT create the default binding to the topic with the same name
        /// as the input queue. Please note that the binding may already exist (possibly from a previous run)
        /// which means that you have do recreate the queue or remove the binding manually.
        /// </summary>
        public RabbitMqMessageQueue DoNotBindDefaultTopicToInputQueue()
        {
            log.Info("Will not bind default topic {0} to input queue {1}", inputQueueName, inputQueueName);
            bindDefaultTopicToInputQueue = false;
            return this;
        }

        /// <summary>
        /// Refrain from declaring the exchange when starting up - allows you to minimize how much the
        /// Rabbit transport may interfere with an already-existing exchange
        /// </summary>
        public RabbitMqMessageQueue DoNotDeclareExchange()
        {
            log.Info("Will not automatically (re)declare exchange");
            ensureExchangeIsDeclared = false;
            return this;
        }

        /// <summary>
        /// Instructs the Rabbit transport to publish messages on the given exchange, 
        /// or to use one exchange per each message type (if no 'default exchange is given').
        /// </summary>
        public RabbitMqMessageQueue UseExchange(string exchangeNameToUse)
        {
            if (exchangeNameToUse != null)
            {
                log.Info("Will use exchanged named {0}", exchangeNameToUse);
                exchangeName = exchangeNameToUse;
            }
            else
            {
                log.Info("Will use one exchange per each message type.");
                exchangeName = null;
            }

            return this;
        }

        /// <summary>
        /// Uses the exchange as input address, by binding it to subscribed type's exchange(s).
        /// </summary>
        /// <param name="exchangeName">Name of the exchange.</param>
        public RabbitMqMessageQueue UseExchangeAsInputAddress(string exchangeName)
        {
            log.Info("Will use exchange named {0} as input address.", exchangeName);
            inputExchangeAddress = "@" + exchangeName;
            return this;
        }

        /// <summary>
        /// Disables Rebus' default behavior of (re)declaring the input exhange when it first interacts with Rabbit.
        /// </summary>
        /// <returns></returns>
        public RabbitMqMessageQueue DoNotDeclareInputExchange()
        {
            ensureInputExchangeIsDeclared = false;
            return this;
        }

        /// <summary>
        /// Configures Rabbit transport to prefetch the specified number of messages
        /// </summary>
        public RabbitMqMessageQueue Prefetch(ushort prefetchCountToSet)
        {
            log.Info("Will set prefetch count to {0} on new connections", prefetchCountToSet);
            prefetchCount = prefetchCountToSet;
            return this;
        }

        /// <summary>
        /// Configures Rabbit transport to create its input queue with the auto-delete
        /// flag set
        /// </summary>
        public RabbitMqMessageQueue AutoDeleteInputQueue()
        {
            log.Info("Will set the autodelete flag on input queue");
            autoDeleteInputQueue = true;
            return this;
        }

        void EnsureInputQueueInitialized(string inputQueueNameToInitialize)
        {
            // If desired queue name is our public exchange, declare our input queue instead.
            // This is needed in order to avoid creating a queue named as our public exchange address
            // we we are using an exchange as our public facing address.
            if (InputQueueAddressIsExchange && inputQueueNameToInitialize == InputQueueAddress)
            {
                inputQueueNameToInitialize = inputQueueName;
            }

            if (CanUseThreadBoundModel)
            {
                InitializeLogicalQueue(inputQueueNameToInitialize, threadBoundModel, false, autoDeleteInputQueue);
            }
            else
            {
                WithConnection(model => InitializeLogicalQueue(inputQueueNameToInitialize, model, false, autoDeleteInputQueue));
            }
        }

        static bool CanUseThreadBoundModel
        {
            get { return threadBoundModel != null && threadBoundModel.IsOpen; }
        }

        IModel GetSenderModel(ITransactionContext context)
        {
            if (context[CurrentModelKey] != null)
            {
                return (IModel)context[CurrentModelKey];
            }

            var model = GetConnection().CreateModel();
            model.TxSelect();
            context[CurrentModelKey] = model;

            context.DoCommit += model.TxCommit;
            context.DoRollback += model.TxRollback;
            context.Cleanup += model.Dispose;

            return model;
        }

        void EstablishSubscriptions(IModel model)
        {
            if (model == null) return;

            foreach (var subscription in subscriptions.Keys)
            {
                EstablishSubscription(model, subscription);
            }
        }

        void EstablishSubscription(IModel model, Type subscription)
        {
            var eventName = GetEventName(subscription);
            var exchange = UsingOneExchangePerMessageTypeRouting ? eventName : ExchangeName;
            var routingKey = UsingOneExchangePerMessageTypeRouting ? "" : eventName;
            var inputAddress = InputQueueAddress.TrimStart('@');

            if (UsingOneExchangePerMessageTypeRouting)
            {
                log.Debug("Declaring fanout exchange for: {0}", exchange);
                model.ExchangeDeclare(exchange, "fanout", true, false, null);
            }

            log.Info("Subscribing {0} to {1}", inputAddress, eventName);
            if (InputQueueAddressIsExchange)
            {
                model.ExchangeBind(inputAddress, exchange, routingKey);
            }
            else
            {
                model.QueueBind(inputAddress, exchange, routingKey);
            }
        }

        void RemoveSubscription(IModel model, Type subscription)
        {
            var eventName = GetEventName(subscription);
            var exchange = UsingOneExchangePerMessageTypeRouting ? eventName : ExchangeName;
            var routingKey = UsingOneExchangePerMessageTypeRouting ? "" : eventName;
            var inputAddress = InputQueueAddress.TrimStart('@');

            log.Info("Unsubscribing {0} from {1}", inputAddress, eventName);
            if (InputQueueAddressIsExchange)
            {
                model.ExchangeUnbind(inputAddress, exchange, routingKey);
            }
            else
            {
                model.QueueUnbind(inputAddress, exchange, routingKey, new Dictionary<string, object>());
            }
        }

        string GetPrettyTypeName(Type messageType)
        {
            if (!messageType.IsGenericType) 
                return messageType.FullName;

            var genericTypeDefinition = messageType.GetGenericTypeDefinition();
            var genericArguments = messageType.GetGenericArguments();

            var fullName = genericTypeDefinition.FullName;
            var substring = fullName.Substring(0, fullName.IndexOf("`", StringComparison.Ordinal));

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

        void InitializeLogicalQueue(string queueName, IModel model, bool asErrorQueue, bool autoDelete = false)
        {
            log.Info("Initializing logical queue '{0}'", queueName);

            var arguments = new Dictionary<string, object> { { "x-ha-policy", "all" } }; //< enable queue mirroring

            log.Debug("Declaring queue '{0}'", queueName);
            model.QueueDeclare(queueName, durable: true,
                               arguments: arguments,
                               autoDelete: autoDelete,
                               exclusive: false);

            // Error queues do not need additional setup.
            // @mookid8000: for "Traditional" Rebus RabbitMQ usage, yes they do - otherwise, failed messages might be published using a topic to which there are no subscribers
            if (asErrorQueue && UsingOneExchangePerMessageTypeRouting) return;

            if (UsingOneExchangePerMessageTypeRouting)
            {
                log.Debug("Queue '{0}' is using one exchange-per-type routing.", queueName);

                if (InputQueueAddressIsExchange && ensureInputExchangeIsDeclared)
                {
                    var exchangeName = InputQueueAddress.TrimStart('@');

                    log.Debug("Declaring (input) exchange '{0}'", InputQueueAddress);
                    model.ExchangeDeclare(exchangeName, ExchangeType.Fanout, true);

                    log.Debug("Binding queue '{0}' to exchange '{0}'", InputQueue, exchangeName);
                    model.QueueBind(InputQueue, exchangeName, "");
                }

                return;
            }

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

        internal void CreateQueue(string queueName, bool asErrorQueue)
        {
            WithConnection(model => InitializeLogicalQueue(queueName, model, asErrorQueue));
        }

        void EnsureThreadBoundModelIsInitialized(ITransactionContext context)
        {
            if (CanUseThreadBoundModel)
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

            InitializeLogicalQueue(inputQueueName, threadBoundModel, false, autoDeleteInputQueue);

            EstablishSubscriptions(threadBoundModel);
        }

        ReceivedTransportMessage GetReceivedTransportMessage(IBasicProperties basicProperties, byte[] body)
        {
            var result = new ReceivedTransportMessage
                {
                    Id = basicProperties != null
                             ? basicProperties.MessageId ?? "(null)"
                             : "(unknown)",
                    Headers = basicProperties != null
                                  ? GetHeaders(basicProperties)
                                  : new Dictionary<string, object>(),
                    Body = body,
                };

            // As this is a recent change, I feel we should alert if a message arrives
            // with different transport-level and header-level IDs. (pruiz)
            if (result.Headers.ContainsKey(Headers.MessageId))
            {
                var headerLevelId = result.Headers[Headers.MessageId];
                if (!string.Equals(headerLevelId, result.Id))
                {
                    log.Warn("Mismatch between transport-level and header-level message's id. " +
                             "This may indicate a software bug, or maybe due to processing of messages " +
                             "generated by previous versions of Rebus. (TLID: {0} - HLID: {1})",
                             result.Id, headerLevelId);
                }
            }

            return result;
        }

        IDictionary<string, object> GetHeaders(IBasicProperties basicProperties)
        {
            var headers = basicProperties.Headers;

            if (headers == null) return new Dictionary<string, object>();

            return headers.ToDictionary(de => (string)de.Key, de => PossiblyDecode(de.Value));
        }

        IBasicProperties GetHeaders(IModel modelToUse, TransportMessageToSend message)
        {
            var props = modelToUse.CreateBasicProperties();

            var persistentMessage = true;

            if (message.Headers != null)
            {
                props.Headers = message.Headers
                    .ToDictionary(kvp => kvp.Key, kvp => PossiblyEncode(kvp.Value));

                if (message.Headers.ContainsKey(Headers.MessageId))
                {
                    // Not sure if message-id is always an string, so let's convert 
                    // whatever the user specified to string and move on. (pruiz)
                    props.MessageId = Convert.ToString(message.Headers[Headers.MessageId]);
                }

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

            // If not already set, specify a unique message's Id.
            if (string.IsNullOrWhiteSpace(props.MessageId)) props.MessageId = Guid.NewGuid().ToString();
            props.SetPersistent(persistentMessage);

            return props;
        }

        object PossiblyEncode(object value)
        {
            if (!(value is string)) return value;

            return Encoding.GetBytes((string)value);
        }

        object PossiblyDecode(object value)
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