using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Messages;
using System.Linq;

namespace Rebus.Bus
{
    /// <summary>
    /// Implements <see cref="IBus"/> as Rebus would do it.
    /// </summary>
    public class RebusBus : IStartableBus, IBus
    {
        static readonly ILog Log = RebusLoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        readonly ISendMessages sendMessages;
        readonly IReceiveMessages receiveMessages;
        readonly IStoreSubscriptions storeSubscriptions;
        readonly IDetermineDestination determineDestination;
        readonly IActivateHandlers activateHandlers;
        readonly ISerializeMessages serializeMessages;
        readonly IStoreSagaData storeSagaData;
        readonly IInspectHandlerPipeline inspectHandlerPipeline;
        readonly List<Worker> workers = new List<Worker>();
        readonly ErrorTracker errorTracker = new ErrorTracker();

        static int rebusIdCounter;
        readonly int rebusId;

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
        public RebusBus(IActivateHandlers activateHandlers, ISendMessages sendMessages, IReceiveMessages receiveMessages, IStoreSubscriptions storeSubscriptions, IStoreSagaData storeSagaData, IDetermineDestination determineDestination, ISerializeMessages serializeMessages, IInspectHandlerPipeline inspectHandlerPipeline)
        {
            this.activateHandlers = activateHandlers;
            this.sendMessages = sendMessages;
            this.receiveMessages = receiveMessages;
            this.storeSubscriptions = storeSubscriptions;
            this.determineDestination = determineDestination;
            this.serializeMessages = serializeMessages;
            this.storeSagaData = storeSagaData;
            this.inspectHandlerPipeline = inspectHandlerPipeline;

            rebusId = Interlocked.Increment(ref rebusIdCounter);

            Log.Info("Rebus bus created");
        }

        public IBus Start()
        {
            const int defaultNumberOfWorkers = 1;

            var numberOfWorkers = RebusConfigurationSection
                .GetConfigurationValueOrDefault(s => s.Workers, defaultNumberOfWorkers)
                .GetValueOrDefault(defaultNumberOfWorkers);

            return Start(numberOfWorkers);
        }

        public RebusBus Start(int numberOfWorkers)
        {
            Log.Info("Initializing bus with {0} workers", numberOfWorkers);
            SetNumberOfWorkers(numberOfWorkers);
            Log.Info("Bus started");
            return this;
        }

        public void Send<TCommand>(TCommand message)
        {
            var destinationEndpoint = GetMessageOwnerEndpointFor(message.GetType());

            InternalSend(destinationEndpoint, message);
        }

        public void Send<TCommand>(string endpoint, TCommand message)
        {
            InternalSend(endpoint, message);
        }

        public void SendLocal<TCommand>(TCommand message)
        {
            var destinationEndpoint = receiveMessages.InputQueue;

            InternalSend(destinationEndpoint, message);
        }

        public void Publish<TEvent>(TEvent message)
        {
            var subscriberEndpoints = storeSubscriptions.GetSubscribers(message.GetType());

            foreach (var subscriberInputQueue in subscriberEndpoints)
            {
                InternalSend(subscriberInputQueue, message);
            }
        }

        public void Reply<TResponse>(TResponse message)
        {
            var returnAddress = MessageContext.GetCurrent().ReturnAddress;

            InternalSend(returnAddress, message);
        }

        public void Subscribe<TMessage>()
        {
            var publisherInputQueue = GetMessageOwnerEndpointFor(typeof(TMessage));

            InternalSubscribe<TMessage>(publisherInputQueue);
        }

        public void Subscribe<TMessage>(string publisherInputQueue)
        {
            InternalSubscribe<TMessage>(publisherInputQueue);
        }

        void InternalSubscribe<TMessage>(string publisherInputQueue)
        {
            var message = new SubscriptionMessage { Type = typeof(TMessage).AssemblyQualifiedName };

            InternalSend(publisherInputQueue, message);
        }

        /// <summary>
        /// Core send method. This should be the only place where calls to the bus'
        /// <see cref="ISendMessages"/> instance gets called, except for when moving
        /// messages to the error queue.
        /// </summary>
        void InternalSend(string endpoint, object message)
        {
            var messageToSend = new Message
                                    {
                                        Messages = new[] { message },
                                        Headers = { { Headers.ReturnAddress, receiveMessages.InputQueue } }
                                    };

            MergeHeaders(messageToSend);

            var transportMessage = serializeMessages.Serialize(messageToSend);

            sendMessages.Send(endpoint, transportMessage);
        }

        void MergeHeaders(Message messageToSend)
        {
            // well - ATM I cannot think of any better way,... we just merge any headers present with those associated with the message
            var allHeaders = messageToSend.Messages
                .Select(msg => HeaderContext.Current.GetHeadersFor(msg))
                .Aggregate(messageToSend.Headers, (d1, d2) => d1.Concat(d2).ToDictionary(k => k.Key, k => k.Value));

            messageToSend.Headers = allHeaders;
        }

        void HandleMessageFailedMaxNumberOfTimes(ReceivedTransportMessage transportMessage, string errorDetail)
        {
            var transportMessageToSend = transportMessage.ToForwardableMessage();

            Log.Warn("Message {0} is forwarded to error queue", transportMessageToSend.Label);

            sendMessages.Send("error", transportMessageToSend);
        }

        public void Dispose()
        {
            workers.ForEach(w => w.Stop());
            workers.ForEach(w => w.Dispose());
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
                                        string.Format("Rebus {0} worker {1}", rebusId, workers.Count + 1));
                workers.Add(worker);
                worker.MessageFailedMaxNumberOfTimes += HandleMessageFailedMaxNumberOfTimes;
                worker.UserException += LogUserException;
                worker.SystemException += LogSystemException;
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
                    workerToRemove.MessageFailedMaxNumberOfTimes -= HandleMessageFailedMaxNumberOfTimes;
                    workerToRemove.UserException -= LogUserException;
                    workerToRemove.SystemException -= LogSystemException;
                    workerToRemove.Dispose();
                }
            }
        }

        void LogSystemException(Worker worker, Exception exception)
        {
            Log.Error(exception, "Unhandled system exception in {0}", worker.WorkerThreadName);
        }

        void LogUserException(Worker worker, Exception exception)
        {
            Log.Warn("User exception in {0}: {1}", worker.WorkerThreadName, exception);
        }

        public void SetNumberOfWorkers(int newNumberOfWorkers)
        {
            while (workers.Count < newNumberOfWorkers) AddWorker();
            while (workers.Count > newNumberOfWorkers) RemoveWorker();
        }
    }

    internal class HeaderContext
    {
        static HeaderContext()
        {
            Current = new HeaderContext();
        }

        public static HeaderContext Current { get; set; }

        readonly ConcurrentDictionary<object, Dictionary<string, string>> headers = new ConcurrentDictionary<object, Dictionary<string, string>>();

        public void AttachHeader(object message, string key, string value)
        {
            var headerDictionary = headers.GetOrAdd(message, msg => new Dictionary<string, string>());

            headerDictionary.Add(key, value);
        }

        public Dictionary<string, string> GetHeadersFor(object message)
        {
            Dictionary<string, string> temp;

            return headers.TryRemove(message, out temp)
                       ? temp
                       : new Dictionary<string, string>();
        }
    }

    public static class MessageExtensions
    {
        public static TMessage AttachHeader<TMessage>(this TMessage message, string key, string value)
        {
            HeaderContext.Current.AttachHeader(message, key, value);
            return message;
        }
    }
}