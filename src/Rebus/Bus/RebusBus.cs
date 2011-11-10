using System;
using System.Collections.Generic;
using System.Reflection;
using Rebus.Extensions;
using Rebus.Messages;
using log4net;

namespace Rebus.Bus
{
    public class RebusBus : IStartableBus, IBus
    {
        static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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

        public RebusBus(IActivateHandlers activateHandlers, 
            ISendMessages sendMessages, 
            IReceiveMessages receiveMessages, 
            IStoreSubscriptions storeSubscriptions, 
            IDetermineDestination determineDestination, 
            ISerializeMessages serializeMessages, 
            IStoreSagaData storeSagaData,
            IInspectHandlerPipeline inspectHandlerPipeline)
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
            Log.InfoFormat("Initializing bus with {0} workers", numberOfWorkers);
            numberOfWorkers.Times(AddWorker);
            Log.Info("Bus started");
            return this;
        }

        public void Send<TMessage>(TMessage message)
        {
            var destinationEndpoint = GetMessageOwnerEndpointFor(message.GetType());

            Send(destinationEndpoint, message);
        }

        public void Send(string endpoint, object message)
        {
            var messageToSend = new Message
                                    {
                                        Messages = new[] {message},
                                        Headers = {{Headers.ReturnAddress, receiveMessages.InputQueue}}
                                    };
            
            var transportMessage = serializeMessages.Serialize(messageToSend);

            sendMessages.Send(endpoint, transportMessage);
        }

        public void Publish<TEvent>(TEvent message)
        {
            var subscriberEndpoints = storeSubscriptions.GetSubscribers(message.GetType());

            foreach (var subscriberInputQueue in subscriberEndpoints)
            {
                Send(subscriberInputQueue, message);
            }
        }

        public void Reply<TReply>(TReply message)
        {
            var messageToSend = new Message
                                    {
                                        Messages = new object[] {message},
                                        Headers = {{Headers.ReturnAddress, receiveMessages.InputQueue}}
                                    };

            var transportMessage = serializeMessages.Serialize(messageToSend);

            var returnAddress = MessageContext.GetCurrent().ReturnAddressOfCurrentTransportMessage;

            sendMessages.Send(returnAddress, transportMessage);
        }

        public void Subscribe<TMessage>()
        {
            var destinationEndpoint = GetMessageOwnerEndpointFor(typeof(TMessage));

            Subscribe<TMessage>(destinationEndpoint);
        }

        public void Subscribe<TMessage>(string publisherInputQueue)
        {
            var message = new SubscriptionMessage {Type = typeof (TMessage).FullName};

            var messageToSend = new Message
                                    {
                                        Messages = new object[] {message},
                                        Headers = {{Headers.ReturnAddress, receiveMessages.InputQueue}}
                                    };

            var transportMessage = serializeMessages.Serialize(messageToSend);

            sendMessages.Send(publisherInputQueue, transportMessage);
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
            worker.UnhandledException += LogUnhandledException;
            worker.Start();
        }

        void LogUnhandledException(Worker worker, Exception exception)
        {
            Log.Error(string.Format("Unhandled exception in worker {0}. This is not good.", worker.WorkerThreadName), exception);
        }

        void HandleMessageFailedMaxNumberOfTimes(TransportMessage transportMessage)
        {
            sendMessages.Send(@".\private$\error", transportMessage);
        }
    }
}