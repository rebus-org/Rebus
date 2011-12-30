using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Rebus.Extensions;
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

            Log.Info("Rebus bus created");
        }

        public IBus Start()
        {
            return Start(1);
        }

        public RebusBus Start(int numberOfWorkers)
        {
            Log.Info("Initializing bus with {0} workers", numberOfWorkers);
            numberOfWorkers.Times(AddWorker);
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
            var message = new SubscriptionMessage {Type = typeof (TMessage).FullName};

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
                                        Messages = new[] {message},
                                        Headers = {{Headers.ReturnAddress, receiveMessages.InputQueue}}
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
            var worker = new Worker(errorTracker,
                                    receiveMessages,
                                    activateHandlers,
                                    storeSubscriptions,
                                    serializeMessages,
                                    storeSagaData,
                                    inspectHandlerPipeline);
            workers.Add(worker);
            worker.MessageFailedMaxNumberOfTimes += HandleMessageFailedMaxNumberOfTimes;
            worker.UserException += LogUserException;
            worker.SystemException += LogSystemException;
            worker.Start();
        }

        void LogSystemException(Worker worker, Exception exception)
        {
            Log.Error(exception, "Unhandled system exception in {0}", worker.WorkerThreadName);
        }

        void LogUserException(Worker worker, Exception exception)
        {
            Log.Error(exception, "User exception in {0}", worker.WorkerThreadName);
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