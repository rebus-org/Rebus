using System;
using System.Collections.Generic;
using System.Threading;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Messages;
using System.Linq;
using Rebus.Shared;
using Rebus.Extensions;
using Rebus.Transports.Msmq;

namespace Rebus.Bus
{
    /// <summary>
    /// Implements <see cref="IBus"/> as Rebus would do it.
    /// </summary>
    public class RebusBus : IStartableBus, IAdvancedBus
    {
        static ILog log;

        static RebusBus()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly ISendMessages sendMessages;
        readonly IReceiveMessages receiveMessages;
        readonly IStoreSubscriptions storeSubscriptions;
        readonly IDetermineDestination determineDestination;
        readonly IActivateHandlers activateHandlers;
        readonly ISerializeMessages serializeMessages;
        readonly IStoreSagaData storeSagaData;
        readonly IInspectHandlerPipeline inspectHandlerPipeline;
        readonly List<Worker> workers = new List<Worker>();
        readonly IErrorTracker errorTracker;
        readonly HeaderContext headerContext = new HeaderContext();
        readonly RebusEvents events = new RebusEvents();
        readonly RebusBatchOperations batch;
        readonly IRebusRouting routing;

        static int rebusIdCounter;
        readonly int rebusId;
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
        /// <param name="determineDestination">Will be used to resolve a destination in cases where the message destination is not explicitly specified as part of a send/subscribe operation.</param>
        /// <param name="serializeMessages">Will be used to serialize and deserialize transport messages.</param>
        /// <param name="inspectHandlerPipeline">Will be called to inspect the pipeline of handlers constructed to handle an incoming message.</param>
        /// <param name="errorTracker">Will be used to track failed delivery attempts.</param>
        public RebusBus(IActivateHandlers activateHandlers, ISendMessages sendMessages, IReceiveMessages receiveMessages, IStoreSubscriptions storeSubscriptions, IStoreSagaData storeSagaData, IDetermineDestination determineDestination, ISerializeMessages serializeMessages, IInspectHandlerPipeline inspectHandlerPipeline, IErrorTracker errorTracker)
        {
            this.activateHandlers = activateHandlers;
            this.sendMessages = sendMessages;
            this.receiveMessages = receiveMessages;
            this.storeSubscriptions = storeSubscriptions;
            this.determineDestination = determineDestination;
            this.serializeMessages = serializeMessages;
            this.storeSagaData = storeSagaData;
            this.inspectHandlerPipeline = inspectHandlerPipeline;
            this.errorTracker = errorTracker;

            batch = new RebusBatchOperations(determineDestination, storeSubscriptions, this);
            routing = new RebusRouting(this);

            rebusId = Interlocked.Increment(ref rebusIdCounter);

            log.Info("Rebus bus created");
        }

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

        public void Send<TCommand>(TCommand message)
        {
            Guard.NotNull(message, "message");

            var destinationEndpoint = GetMessageOwnerEndpointFor(message.GetType());

            InternalSend(destinationEndpoint, new List<object> { message });
        }

        public void SendLocal<TCommand>(TCommand message)
        {
            Guard.NotNull(message, "message");

            EnsureBusModeIsNot(BusMode.OneWayClientMode, "You cannot SendLocal when running in one-way client mode, because there's no way for the bus to receive the message you're sending.");

            var destinationEndpoint = receiveMessages.InputQueue;

            InternalSend(destinationEndpoint, new List<object> { message });
        }

        public void Publish<TEvent>(TEvent message)
        {
            Guard.NotNull(message, "message");

            var multicastTransport = sendMessages as IMulticastTransport;
            if (multicastTransport != null && multicastTransport.ManagesSubscriptions)
            {
                AttachHeader(message, Headers.Multicast, "");
                InternalSend(typeof(TEvent).FullName, new List<object> { message });
                return;
            }

            var subscriberEndpoints = storeSubscriptions.GetSubscribers(message.GetType());

            foreach (var subscriberInputQueue in subscriberEndpoints)
            {
                InternalSend(subscriberInputQueue, new List<object> { message });
            }
        }

        public IRebusEvents Events
        {
            get { return events; }
        }

        public IRebusBatchOperations Batch
        {
            get { return batch; }
        }

        public IRebusRouting Routing
        {
            get { return routing; }
        }

        public void Reply<TResponse>(TResponse message)
        {
            Guard.NotNull(message, "message");

            InternalReply(new List<object> { message });
        }

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

        public void Defer(TimeSpan delay, object message)
        {
            Guard.NotNull(message, "message");
            Guard.GreaterThanOrEqual(delay, TimeSpan.FromSeconds(0), "delay");

            var customData = TimeoutReplyHandler.Serialize(message);

            var messages = new List<object>
                               {
                                   new TimeoutRequest
                                       {
                                           Timeout = delay,
                                           CustomData = customData,
                                           CorrelationId = TimeoutReplyHandler.TimeoutReplySecretCorrelationId
                                       }
                               };

            InternalSend("rebus.timeout", messages);
        }

        public void AttachHeader(object message, string key, string value)
        {
            Guard.NotNull(message, "message");
            Guard.NotNull(key, "key");

            headerContext.AttachHeader(message, key, value);
        }

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

            if (receiveMessages is MsmqConfigurationExtension.OneWayClientGag)
            {
                log.Info("Bus will be started in the experimental one-way client mode");
                numberOfWorkers = 0;
                busMode = BusMode.OneWayClientMode;
            }

            log.Info("Initializing bus with {0} workers", numberOfWorkers);

            SetNumberOfWorkers(numberOfWorkers);
            started = true;

            log.Info("Bus started");
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
for the ONE-WAY CLIENT MODE of the bus, which is what you automatically get if
you omit the inputQueue, errorQueue and workers attributes of the Rebus XML
element)"));
            }

            messages.ForEach(m => events.RaiseMessageSent(this, destination, m));

            var messageToSend = new Message { Messages = messages.ToArray(), };
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

        internal void InternalSend(string destination, Message messageToSend)
        {
            log.Info("Sending {0} to {1}", string.Join("+", messageToSend.Messages), destination);
            var transportMessage = serializeMessages.Serialize(messageToSend);

            sendMessages.Send(destination, transportMessage);
        }

        IDictionary<string, string> MergeHeaders(Message messageToSend)
        {
            var transportMessageHeaders = messageToSend.Headers.Clone();

            var messages = messageToSend.Messages
                .Select(m => new Tuple<object, Dictionary<string, string>>(m, headerContext.GetHeadersFor(m)))
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

        void AssertReturnAddressIsNotInconsistent(List<Tuple<object, Dictionary<string, string>>> messages)
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

        void AssertTimeToBeReceivedIsNotInconsistent(List<Tuple<object, Dictionary<string, string>>> messages)
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
                sendMessages.Send(errorTracker.ErrorQueueAddress, transportMessageToSend);
            }
            catch (Exception e)
            {
                log.Error(e, "Wanted to move message with id {0} to the error queue, but an exception occurred!", receivedTransportMessage.Id);

                // what to do? we need to throw again, or the message will not be rolled back and will thus be lost
                // - but we want to avoid thrashing, so we just log the badness and relax a little bit - that's
                // probably the best we can do
                Thread.Sleep(300);

                throw;
            }
        }

        public void Dispose()
        {
            SetNumberOfWorkers(0);

            var disposables = new object[]
                {
                    headerContext, sendMessages, receiveMessages,
                    storeSubscriptions, storeSagaData
                }
                .Where(r => !ReferenceEquals(null, r))
                .OfType<IDisposable>()
                .Distinct();

            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }

        string GetMessageOwnerEndpointFor(Type messageType)
        {
            return determineDestination.GetEndpointFor(messageType);
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
                                        new DeferredMessageReDispatcher(this));
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

                    workerToRemove.MessageFailedMaxNumberOfTimes -= HandleMessageFailedMaxNumberOfTimes;
                    workerToRemove.UserException -= LogUserException;
                    workerToRemove.SystemException -= LogSystemException;
                    workerToRemove.BeforeTransportMessage -= RaiseBeforeTransportMessage;
                    workerToRemove.AfterTransportMessage -= RaiseAfterTransportMessage;
                    workerToRemove.PoisonMessage -= RaisePosionMessage;
                    workerToRemove.BeforeMessage -= RaiseBeforeMessage;
                    workerToRemove.AfterMessage -= RaiseAfterMessage;
                    workerToRemove.UncorrelatedMessage -= RaiseUncorrelatedMessage;
                }
            }
        }

        void RaiseUncorrelatedMessage(object message, Saga saga)
        {
            events.RaiseUncorrelatedMessage(message, saga);
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

        void RaisePosionMessage(ReceivedTransportMessage transportMessage)
        {
            events.RaisePoisonMessage(this, transportMessage);
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
            internal readonly List<Tuple<WeakReference, Dictionary<string, string>>> Headers = new List<Tuple<WeakReference, Dictionary<string, string>>>();

            internal readonly System.Timers.Timer CleanupTimer;

            public HeaderContext()
            {
                CleanupTimer = new System.Timers.Timer { Interval = 1000 };
                CleanupTimer.Elapsed += (o, ea) => Headers.RemoveDeadReferences();
            }

            public void AttachHeader(object message, string key, string value)
            {
                var headerDictionary = Headers.GetOrAdd(message, () => new Dictionary<string, string>());

                headerDictionary.Add(key, value);
            }

            public Dictionary<string, string> GetHeadersFor(object message)
            {
                Dictionary<string, string> temp;

                var headersForThisMessage = Headers.TryGetValue(message, out temp)
                                                ? temp
                                                : new Dictionary<string, string>();

                return headersForThisMessage;
            }

            public void Tick()
            {
                Headers.RemoveDeadReferences();
            }

            public void Dispose()
            {
                CleanupTimer.Dispose();
            }
        }
    }

    public interface IMulticastTransport
    {
        bool ManagesSubscriptions { get; }

        void Subscribe(Type messageType, string inputQueueAddress);

        void Unsubscribe(Type messageType, string inputQueueAddress);
    }
}