using System;
using System.Collections.Generic;
using System.Threading;
using System.Transactions;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Messages;
using System.Linq;
using Rebus.Persistence.SqlServer;
using Rebus.Shared;
using Rebus.Extensions;
using Rebus.Timeout;
using Rebus.Transports;

namespace Rebus.Bus
{
    /// <summary>
    /// Implements <see cref="IBus"/> as Rebus would do it.
    /// </summary>
    public class RebusBus : IStartableBus, IBus, IAdvancedBus
    {
        static ILog log;

        static RebusBus()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly ISendMessages sendMessages;
        readonly IReceiveMessages receiveMessages;
        readonly IStoreSubscriptions storeSubscriptions;
        readonly IDetermineMessageOwnership determineMessageOwnership;
        readonly IActivateHandlers activateHandlers;
        readonly ISerializeMessages serializeMessages;
        readonly IStoreSagaData storeSagaData;
        readonly IInspectHandlerPipeline inspectHandlerPipeline;
        readonly List<Worker> workers = new List<Worker>();
        readonly IErrorTracker errorTracker;
        readonly IStoreTimeouts storeTimeouts;
        readonly ConfigureAdditionalBehavior configureAdditionalBehavior;
        readonly HeaderContext headerContext = new HeaderContext();
        readonly RebusEvents events = new RebusEvents();
        readonly RebusBatchOperations batch;
        readonly DueTimeoutScheduler dueTimeoutScheduler;
        readonly IRebusRouting routing;

        static int rebusIdCounter;
        readonly int rebusId;
        readonly string timeoutManagerAddress;
        bool started;
        BusMode busMode;

        /// <summary>
        /// Constructs the bus with the specified ways of achieving its goals.
        /// </summary>
        /// <param name="activateHandlers">The bus will use this to construct handlers for received messages.</param>
        /// <param name="sendMessages">Will be used to send transport messages when you send, publish, and reply.</param>
        /// <param name="receiveMessages">Will be used to receive transport messages. If the bus is configured to run with multiple threads, this one should be reentrant.</param>
        /// <param name="storeSubscriptions">Will be used to store subscription information. Is only relevant if the bus is a publisher, i.e. it publishes messages and other services assume they can subscribe to its messages.</param>
        /// <param name="storeSagaData">Will be used to store saga data. Is only relevant if one or more handlers are derived from <see cref="Saga"/>.</param>
        /// <param name="determineMessageOwnership">Will be used to resolve a destination in cases where the message destination is not explicitly specified as part of a send/subscribe operation.</param>
        /// <param name="serializeMessages">Will be used to serialize and deserialize transport messages.</param>
        /// <param name="inspectHandlerPipeline">Will be called to inspect the pipeline of handlers constructed to handle an incoming message.</param>
        /// <param name="errorTracker">Will be used to track failed delivery attempts.</param>
        /// <param name="storeTimeouts">Optionally provides an internal timeout manager to be used instead of sending timeout requests to an external timeout manager</param>
        /// <param name="configureAdditionalBehavior"></param>
        public RebusBus(IActivateHandlers activateHandlers, ISendMessages sendMessages, IReceiveMessages receiveMessages, IStoreSubscriptions storeSubscriptions, IStoreSagaData storeSagaData, IDetermineMessageOwnership determineMessageOwnership, ISerializeMessages serializeMessages, IInspectHandlerPipeline inspectHandlerPipeline, IErrorTracker errorTracker, IStoreTimeouts storeTimeouts, ConfigureAdditionalBehavior configureAdditionalBehavior)
        {
            this.activateHandlers = activateHandlers;
            this.sendMessages = sendMessages;
            this.receiveMessages = receiveMessages;
            this.storeSubscriptions = storeSubscriptions;
            this.determineMessageOwnership = determineMessageOwnership;
            this.serializeMessages = serializeMessages;
            this.storeSagaData = storeSagaData;
            this.inspectHandlerPipeline = inspectHandlerPipeline;
            this.errorTracker = errorTracker;
            this.storeTimeouts = storeTimeouts;
            this.configureAdditionalBehavior = configureAdditionalBehavior;

            batch = new RebusBatchOperations(determineMessageOwnership, storeSubscriptions, this);
            routing = new RebusRouting(this);

            rebusId = Interlocked.Increment(ref rebusIdCounter);

            log.Info("Rebus bus {0} created", rebusId);

            if (storeTimeouts == null)
            {
                var timeoutManagerEndpointAddress = RebusConfigurationSection
                    .GetConfigurationValueOrDefault(s => s.TimeoutManagerAddress, "rebus.timeout");

                log.Info("Using timeout manager with input queue {0}", timeoutManagerEndpointAddress);
                timeoutManagerAddress = timeoutManagerEndpointAddress;
            }
            else
            {
                log.Info("Using local timeout manager");
                timeoutManagerAddress = this.receiveMessages.InputQueue;
                dueTimeoutScheduler = new DueTimeoutScheduler(storeTimeouts, new DeferredMessageReDispatcher(this));
            }
        }

        IEnumerable<object> InjectedServices()
        {
            return new object[]
                       {
                           activateHandlers,
                           headerContext, sendMessages, receiveMessages,
                           storeSubscriptions, storeSagaData,
                           dueTimeoutScheduler, determineMessageOwnership,
                           serializeMessages,
                           inspectHandlerPipeline,
                           errorTracker,
                           storeTimeouts
                       }
                .Where(r => !ReferenceEquals(null, r));
        }

        /// <summary>
        /// Starts the bus
        /// </summary>
        public IBus Start()
        {
            const int defaultNumberOfWorkers = 1;

            var numberOfWorkers = RebusConfigurationSection
                .GetConfigurationValueOrDefault(s => s.Workers, defaultNumberOfWorkers)
                .GetValueOrDefault(defaultNumberOfWorkers);

            InternalStart(numberOfWorkers);

            return this;
        }

        /// <summary>
        /// Starts the <see cref="RebusBus"/> with the specified number of worker threads.
        /// </summary>
        public RebusBus Start(int numberOfWorkers)
        {
            Guard.GreaterThanOrEqual(numberOfWorkers, 0, "numberOfWorkers");

            InternalStart(numberOfWorkers);

            return this;
        }


        /// <summary>
        /// Sends the specified message to the destination as specified by the currently
        /// used implementation of <see cref="IDetermineMessageOwnership"/>.
        /// </summary>
        public void Send<TCommand>(TCommand message)
        {
            Guard.NotNull(message, "message");

            var destinationEndpoint = GetMessageOwnerEndpointFor(message.GetType());

            PossiblyAttachSagaIdToRequest(message);

            InternalSend(destinationEndpoint, new List<object> { message });
        }

        /// <summary>
        /// Sends the specified message to the bus' own input queue.
        /// </summary>
        public void SendLocal<TCommand>(TCommand message)
        {
            Guard.NotNull(message, "message");

            EnsureBusModeIsNot(BusMode.OneWayClientMode, "You cannot SendLocal when running in one-way client mode, because there's no way for the bus to receive the message you're sending.");

            var destinationEndpoint = receiveMessages.InputQueue;

            PossiblyAttachSagaIdToRequest(message);

            InternalSend(destinationEndpoint, new List<object> { message });
        }

        /// <summary>
        /// Publishes the specified event message to all endpoints that are currently subscribed.
        /// The publisher should have some kind of <see cref="IStoreSubscriptions"/> implementation,
        /// preferably a durable implementation like e.g. <see cref="SqlServerSubscriptionStorage"/>.
        /// </summary>
        public void Publish<TEvent>(TEvent message)
        {
            Guard.NotNull(message, "message");

            var multicastTransport = sendMessages as IMulticastTransport;
            if (multicastTransport != null && multicastTransport.ManagesSubscriptions)
            {
                AttachHeader(message, Headers.Multicast, "");
                InternalSend(multicastTransport.GetEventName(message.GetType()), new List<object> { message });
                return;
            }

            var subscriberEndpoints = storeSubscriptions.GetSubscribers(message.GetType());

            foreach (var subscriberInputQueue in subscriberEndpoints)
            {
                InternalSend(subscriberInputQueue, new List<object> { message });
            }
        }

        internal void PossiblyAttachSagaIdToRequest<TCommand>(TCommand message)
        {
            if (MessageContext.HasCurrent)
            {
                var messageContext = MessageContext.GetCurrent();

                if (messageContext.Items.ContainsKey(SagaContext.SagaContextItemKey))
                {
                    var sagaContext = (SagaContext)messageContext.Items[SagaContext.SagaContextItemKey];

                    AttachHeader(message, Headers.AutoCorrelationSagaId, sagaContext.Id.ToString());
                }
            }
        }

        /// <summary>
        /// Gives access to all the different event hooks that Rebus exposes.
        /// </summary>
        public IRebusEvents Events
        {
            get { return events; }
        }

        /// <summary>
        /// Gives access to Rebus' batch operations.
        /// </summary>
        public IRebusBatchOperations Batch
        {
            get { return batch; }
        }

        /// <summary>
        /// Gives access to Rebus' routing operations.
        /// </summary>
        public IRebusRouting Routing
        {
            get { return routing; }
        }

        /// <summary>
        /// Sends a reply back to the sender of the message currently being handled. Can only
        /// be called when a <see cref="MessageContext"/> has been established, which happens
        /// during the handling of an incoming message.
        /// </summary>
        public void Reply<TResponse>(TResponse message)
        {
            Guard.NotNull(message, "message");

            InternalReply(new List<object> { message });
        }

        /// <summary>
        /// Sends a subscription request for <typeparamref name="TEvent"/> to the destination as
        /// specified by the currently used implementation of <see cref="IDetermineMessageOwnership"/>.
        /// </summary>
        public void Subscribe<TEvent>()
        {
            var multicastTransport = sendMessages as IMulticastTransport;

            if (multicastTransport != null && multicastTransport.ManagesSubscriptions)
            {
                multicastTransport.Subscribe(typeof(TEvent), receiveMessages.InputQueueAddress);
                return;
            }

            var publisherInputQueue = GetMessageOwnerEndpointFor(typeof(TEvent));

            InternalSubscribe<TEvent>(publisherInputQueue);
        }

        /// <summary>
        /// Sends an unsubscription request for <typeparamref name="TEvent"/> to the destination as
        /// specified by the currently used implementation of <see cref="IDetermineMessageOwnership"/>.
        /// </summary>
        public void Unsubscribe<TEvent>()
        {
            var multicastTransport = sendMessages as IMulticastTransport;

            if (multicastTransport != null && multicastTransport.ManagesSubscriptions)
            {
                multicastTransport.Unsubscribe(typeof(TEvent), receiveMessages.InputQueueAddress);
                return;
            }

            var publisherInputQueue = GetMessageOwnerEndpointFor(typeof(TEvent));

            InternalUnsubscribe<TEvent>(publisherInputQueue);
        }

        /// <summary>
        /// Sets the number of workers in this <see cref="RebusBus"/> to the specified
        /// number. The number of workers must be greater than or equal to 0.
        /// </summary>
        public void SetNumberOfWorkers(int newNumberOfWorkers)
        {
            Guard.GreaterThanOrEqual(newNumberOfWorkers, 0, "newNumberOfWorkers");

            while (workers.Count < newNumberOfWorkers) AddWorker();
            while (workers.Count > newNumberOfWorkers) RemoveWorker();
        }

        /// <summary>
        /// Sends the message to the timeout manager, which will send it back after the specified
        /// time span has elapsed. Note that you must have a running timeout manager for this to
        /// work.
        /// </summary>
        public void Defer(TimeSpan delay, object message)
        {
            Guard.NotNull(message, "message");
            Guard.GreaterThanOrEqual(delay, TimeSpan.FromSeconds(0), "delay");

            var customData = TimeoutReplyHandler.Serialize(message);

            var timeoutRequest = new TimeoutRequest
                {
                    Timeout = delay,
                    CustomData = customData,
                    CorrelationId = TimeoutReplyHandler.TimeoutReplySecretCorrelationId
                };

            if (MessageContext.HasCurrent)
            {
                var messageContext = MessageContext.GetCurrent();

                // if we're in a saga context, be nice and set the saga ID automatically
                if (messageContext.Items.ContainsKey(SagaContext.SagaContextItemKey))
                {
                    var sagaContext = ((SagaContext)messageContext.Items[SagaContext.SagaContextItemKey]);
                    timeoutRequest.SagaId = sagaContext.Id;
                }
            }

            var messages = new List<object> { timeoutRequest };

            InternalSend(timeoutManagerAddress, messages);
        }

        /// <summary>
        /// Attaches to the specified message a header with the given key and value. The header will
        /// be associated with the message, and will be supplied when the message is sent - even if
        /// it is sent multiple times.
        /// </summary>
        public void AttachHeader(object message, string key, string value)
        {
            Guard.NotNull(message, "message");
            Guard.NotNull(key, "key");

            headerContext.AttachHeader(message, key, value);
        }

        /// <summary>
        /// Gain access to more advanced and less commonly used features of the bus
        /// </summary>
        public IAdvancedBus Advanced { get { return this; } }

        internal void InternalSubscribe<TMessage>(string publisherInputQueue)
        {
            SendSubscriptionMessage<TMessage>(publisherInputQueue, SubscribeAction.Subscribe);
        }

        internal void InternalUnsubscribe<TMessage>(string publisherInputQueue)
        {
            SendSubscriptionMessage<TMessage>(publisherInputQueue, SubscribeAction.Unsubscribe);
        }

        internal void SendSubscriptionMessage<TMessage>(string destinationQueue, SubscribeAction subscribeAction)
        {
            EnsureBusModeIsNot(BusMode.OneWayClientMode,
                               "You cannot Subscribe/Unsubscribe when running in one-way client mode, because there's no way for the bus to receive anything from the publisher.");

            var message = new SubscriptionMessage
                {
                    Type = typeof(TMessage).AssemblyQualifiedName,
                    Action = subscribeAction,
                };

            InternalSend(destinationQueue, new List<object> { message });
        }

        internal void InternalStart(int numberOfWorkers)
        {
            if (started)
            {
                throw new InvalidOperationException(string.Format(@"Bus has already been started - cannot start bus twice!

Not that it actually matters, I mean we _could_ just ignore subsequent calls to Start() if we wanted to - but if you're calling Start() multiple times it's most likely a sign that something is wrong, i.e. you might be running you app initialization code more than once, etc."));
            }

            if (receiveMessages is OneWayClientGag)
            {
                log.Info("Rebus {0} will be started in one-way client mode", rebusId);
                numberOfWorkers = 0;
                busMode = BusMode.OneWayClientMode;
            }

            InitializeServicesThatMustBeInitialized();

            log.Info("Initializing bus with {0} workers", numberOfWorkers);

            SetNumberOfWorkers(numberOfWorkers);
            started = true;

            log.Info("Bus started");
        }

        void InitializeServicesThatMustBeInitialized()
        {
            foreach (var mustBeInitialized in InjectedServices().OfType<INeedInitializationBeforeStart>())
            {
                log.Info("Initializing {0}", mustBeInitialized.GetType());
                mustBeInitialized.Initialize();
            }
        }

        internal void InternalReply(List<object> messages)
        {
            if (!MessageContext.HasCurrent)
            {
                var errorMessage = string.Format("You seem to have called Reply outside of a message handler! You can" +
                                                 " only reply to messages within a message handler while handling a" +
                                                 " message, because that's the only place where there's a message" +
                                                 " context in place.");

                throw new InvalidOperationException(errorMessage);
            }

            var messageContext = MessageContext.GetCurrent();
            var returnAddress = messageContext.ReturnAddress;

            if (string.IsNullOrEmpty(returnAddress))
            {
                var errorMessage =
                    string.Format("Message with ID {0} cannot be replied to, because the {1} header is empty." +
                                  " This might be an indication that the requestor is not expecting a reply," +
                                  " e.g. if the requestor is in one-way client mode. If you want to offload a" +
                                  " reply to someone, you can make the requestor include the {1} header manually," +
                                  " using the address of another service as the value - this way, replies will" +
                                  " be sent to a third party, that can take action.",
                                  messageContext.TransportMessageId,
                                  Headers.ReturnAddress);

                throw new InvalidOperationException(errorMessage);
            }

            // transfer auto-correlation saga id to reply if it is present in the current message context
            if (messageContext.Headers.ContainsKey(Headers.AutoCorrelationSagaId))
            {
                AttachHeader(messages.First(), Headers.AutoCorrelationSagaId, messageContext.Headers[Headers.AutoCorrelationSagaId].ToString());
            }

            InternalSend(returnAddress, messages);
        }

        /// <summary>
        /// Core send method. This should be the only place where calls to the bus'
        /// <see cref="ISendMessages"/> instance gets called, except for when moving
        /// messages to the error queue. This method will bundle the specified batch
        /// of messages inside one single transport message, which it will send.
        /// </summary>
        internal void InternalSend(string destination, List<object> messages)
        {
            if (!started)
            {
                throw new InvalidOperationException(
                    string.Format(
                        @"Cannot send messages with a bus that has not been started!

Or actually, you _could_ - but it would most likely be an error if you were
using a bus without starting it... if you mean only to SEND messages, never
RECEIVE anything, then you're not looking for an unstarted bus, you're looking
for the ONE-WAY CLIENT MODE of the bus, which is what you get if
you omit the inputQueue, errorQueue and workers attributes of the Rebus XML
element and use e.g. .Transport(t => t.UseMsmqInOneWayClientMode())"));
            }

            messages.ForEach(m => events.RaiseMessageSent(this, destination, m));

            var messageToSend = new Message { Messages = messages.Select(MutateOutgoing).ToArray(), };
            var headers = MergeHeaders(messageToSend);
            if (!headers.ContainsKey(Headers.ReturnAddress))
            {
                if (busMode != BusMode.OneWayClientMode)
                {
                    headers[Headers.ReturnAddress] = receiveMessages.InputQueueAddress;
                }
            }

            messageToSend.Headers = headers;

            InternalSend(destination, messageToSend);
        }

        object MutateOutgoing(object msg)
        {
            return Events.MessageMutators.Aggregate(msg, (current, mutator) => mutator.MutateOutgoing(current));
        }

        internal void InternalSend(string destination, Message messageToSend)
        {
            log.Info("Sending {0} to {1}", string.Join("+", messageToSend.Messages), destination);
            var transportMessage = serializeMessages.Serialize(messageToSend);

            sendMessages.Send(destination, transportMessage, GetTransactionContext());
        }

        ITransactionContext GetTransactionContext()
        {
            if (TransactionContext.Current != null) return TransactionContext.Current;

            if (Transaction.Current == null) return new NoTransaction();

            return new AmbientTransactionContext();
        }

        IDictionary<string, object> MergeHeaders(Message messageToSend)
        {
            var transportMessageHeaders = messageToSend.Headers.Clone();

            var messages = messageToSend.Messages
                .Select(m => new Tuple<object, Dictionary<string, object>>(m, headerContext.GetHeadersFor(m)))
                .ToList();

            AssertTimeToBeReceivedIsNotInconsistent(messages);
            AssertReturnAddressIsNotInconsistent(messages);

            // stupid trivial merge of all headers - will not detect inconsistensies at this point,
            // and duplicated headers will be overwritten, so it's pretty important that silly
            // stuff has been prevented
            foreach (var header in messages.SelectMany(m => m.Item2))
            {
                transportMessageHeaders[header.Key] = header.Value;
            }

            return transportMessageHeaders;
        }

        void AssertReturnAddressIsNotInconsistent(List<Tuple<object, Dictionary<string, object>>> messages)
        {
            if (messages.Any(m => m.Item2.ContainsKey(Headers.ReturnAddress)))
            {
                var returnAddresses = messages.Select(m => m.Item2[Headers.ReturnAddress]).Distinct();

                if (returnAddresses.Count() > 1)
                {
                    throw new InconsistentReturnAddressException("These return addresses were specified: {0}", string.Join(", ", returnAddresses));
                }
            }
        }

        void AssertTimeToBeReceivedIsNotInconsistent(List<Tuple<object, Dictionary<string, object>>> messages)
        {
            if (messages.Any(m => m.Item2.ContainsKey(Headers.TimeToBeReceived)))
            {
                // assert all messages have the header
                if (!(messages.All(m => m.Item2.ContainsKey(Headers.TimeToBeReceived))))
                {
                    throw new InconsistentTimeToBeReceivedException("Not all messages in the batch had an attached time to be received header!");
                }

                // assert all values are the same
                var timesToBeReceived = messages.Select(m => m.Item2[Headers.TimeToBeReceived]).Distinct();
                if (timesToBeReceived.Count() > 1)
                {
                    throw new InconsistentTimeToBeReceivedException("These times to be received were specified: {0}", string.Join(", ", timesToBeReceived));
                }
            }
        }

        void HandleMessageFailedMaxNumberOfTimes(ReceivedTransportMessage receivedTransportMessage, string errorDetail)
        {
            var transportMessageToSend = receivedTransportMessage.ToForwardableMessage();

            log.Warn("Message {0} is forwarded to error queue", transportMessageToSend.Label);

            transportMessageToSend.Headers[Headers.SourceQueue] = receiveMessages.InputQueueAddress;
            transportMessageToSend.Headers[Headers.ErrorMessage] = errorDetail;

            try
            {
                sendMessages.Send(errorTracker.ErrorQueueAddress, transportMessageToSend, GetTransactionContext());
            }
            catch (Exception e)
            {
                log.Error(e, "An error occurred while attempting to move message with id {0} to the error queue '{1}'",
                          receivedTransportMessage.Id, errorTracker.ErrorQueueAddress);

                // what to do? we need to throw again, or the message will not be rolled back and will thus be lost
                // - but we want to avoid thrashing, so we just log the badness and relax a little bit - that's
                // probably the best we can do
                Thread.Sleep(TimeSpan.FromSeconds(1));

                throw;
            }
        }

        /// <summary>
        /// Stops all workers, waits until they are stopped, disposes all implementations of abstractions that
        /// implement <see cref="IDisposable"/>
        /// </summary>
        public void Dispose()
        {
            // redundant optimization: just tell all workers to stop at the same time
            workers.AsParallel().ForAll(w => w.Stop());

            SetNumberOfWorkers(0);

            var disposables = InjectedServices()
                .Except(new object[] {activateHandlers})
                .OfType<IDisposable>()
                .Distinct();

            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }

        string GetMessageOwnerEndpointFor(Type messageType)
        {
            return determineMessageOwnership.GetEndpointFor(messageType);
        }

        void AddWorker()
        {
            lock (workers)
            {
                var worker = new Worker(errorTracker,
                                        receiveMessages,
                                        activateHandlers,
                                        storeSubscriptions,
                                        serializeMessages,
                                        storeSagaData,
                                        inspectHandlerPipeline,
                                        string.Format("Rebus {0} worker {1}", rebusId, workers.Count + 1),
                                        new DeferredMessageReDispatcher(this),
                                        new IncomingMessageMutatorPipeline(Events),
                                        storeTimeouts,
                                        events.UnitOfWorkManagers,
                                        configureAdditionalBehavior);
                workers.Add(worker);
                worker.MessageFailedMaxNumberOfTimes += HandleMessageFailedMaxNumberOfTimes;
                worker.UserException += LogUserException;
                worker.SystemException += LogSystemException;
                worker.BeforeTransportMessage += RaiseBeforeTransportMessage;
                worker.AfterTransportMessage += RaiseAfterTransportMessage;
                worker.PoisonMessage += RaisePosionMessage;
                worker.BeforeMessage += RaiseBeforeMessage;
                worker.AfterMessage += RaiseAfterMessage;
                worker.UncorrelatedMessage += RaiseUncorrelatedMessage;
                worker.MessageContextEstablished += RaiseMessageContextEstablished;
                worker.Start();
            }
        }

        void RemoveWorker()
        {
            lock (workers)
            {
                if (workers.Count == 0) return;
                var workerToRemove = workers.Last();
                workers.Remove(workerToRemove);

                try
                {
                    workerToRemove.Stop();
                }
                finally
                {
                    try
                    {
                        workerToRemove.Dispose();
                    }
                    catch (Exception e)
                    {
                        log.Error(e, "An error occurred while disposing {0}", workerToRemove.WorkerThreadName);
                    }
                }
            }
        }

        void RaiseMessageContextEstablished(IMessageContext messageContext)
        {
            events.RaiseMessageContextEstablished(this, messageContext);
        }

        void RaiseUncorrelatedMessage(object message, Saga saga)
        {
            events.RaiseUncorrelatedMessage(this, message, saga);
        }

        void RaiseBeforeMessage(object message)
        {
            events.RaiseBeforeMessage(this, message);
        }

        void RaiseAfterMessage(Exception exception, object message)
        {
            events.RaiseAfterMessage(this, exception, message);
        }

        void RaiseBeforeTransportMessage(ReceivedTransportMessage transportMessage)
        {
            events.RaiseBeforeTransportMessage(this, transportMessage);
        }

        void RaiseAfterTransportMessage(Exception exception, ReceivedTransportMessage transportMessage)
        {
            events.RaiseAfterTransportMessage(this, exception, transportMessage);
        }

        void RaisePosionMessage(ReceivedTransportMessage transportMessage, PoisonMessageInfo poisonMessageInfo)
        {
            events.RaisePoisonMessage(this, transportMessage, poisonMessageInfo);
        }

        void LogSystemException(Worker worker, Exception exception)
        {
            log.Error(exception, "Unhandled system exception in {0}", worker.WorkerThreadName);
        }

        void LogUserException(Worker worker, Exception exception)
        {
            log.Warn("User exception in {0}: {1}", worker.WorkerThreadName, exception);
        }

        void EnsureBusModeIsNot(BusMode busModeToAvoid, string message, params object[] objs)
        {
            if (busMode != busModeToAvoid) return;

            throw new InvalidOperationException(string.Format(message, objs));
        }

        internal class HeaderContext
        {
            internal readonly List<Tuple<WeakReference, Dictionary<string, object>>> headers = new List<Tuple<WeakReference, Dictionary<string, object>>>();
            internal readonly System.Timers.Timer cleanupTimer;

            public HeaderContext()
            {
                cleanupTimer = new System.Timers.Timer { Interval = TimeSpan.FromSeconds(1).TotalMilliseconds };
                cleanupTimer.Elapsed += (o, ea) => headers.RemoveDeadReferences();
            }

            public void AttachHeader(object message, string key, string value)
            {
                var headerDictionary = headers.GetOrAdd(message, () => new Dictionary<string, object>());

                headerDictionary[key] = value;
            }

            public Dictionary<string, object> GetHeadersFor(object message)
            {
                Dictionary<string, object> temp;

                var headersForThisMessage = headers.TryGetValue(message, out temp)
                                                ? temp
                                                : new Dictionary<string, object>();

                return headersForThisMessage;
            }

            public void Tick()
            {
                headers.RemoveDeadReferences();
            }

            public void Dispose()
            {
                cleanupTimer.Dispose();
            }
        }
    }
}