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
        readonly List<Worker> workers = new List<Worker>();
        readonly ErrorTracker errorTracker = new ErrorTracker();

        public RebusBus(IActivateHandlers activateHandlers,
            ISendMessages sendMessages,
            IReceiveMessages receiveMessages,
            IStoreSubscriptions storeSubscriptions,
            IDetermineDestination determineDestination, ISerializeMessages serializeMessages)
        {
            this.activateHandlers = activateHandlers;
            this.sendMessages = sendMessages;
            this.receiveMessages = receiveMessages;
            this.storeSubscriptions = storeSubscriptions;
            this.determineDestination = determineDestination;
            this.serializeMessages = serializeMessages;
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
            Send(GetDestinationEndpointFor(message.GetType()), message);
        }

        string GetDestinationEndpointFor(Type messageType)
        {
            return determineDestination.GetEndpointFor(messageType);
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
            foreach (var subscriberInputQueue in storeSubscriptions.GetSubscribers(message.GetType()))
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
            
            sendMessages.Send(GetReturnAddress(), transportMessage);
        }

        public void Subscribe<TMessage>()
        {
            Subscribe<TMessage>(GetDestinationEndpointFor(typeof(TMessage)));
        }

        public void Subscribe<TMessage>(string publisherInputQueue)
        {
            var messageToSend = new Message
                                    {
                                        Messages = new object[]
                                                       {
                                                           new SubscriptionMessage
                                                               {
                                                                   Type = typeof (TMessage).FullName,
                                                               }
                                                       },
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

        void AddWorker()
        {
            var worker = new Worker(errorTracker, receiveMessages, activateHandlers, storeSubscriptions, serializeMessages);
            workers.Add(worker);
            worker.MessageFailedMaxNumberOfTimes += HandleMessageFailedMaxNumberOfTimes;
            worker.Start();
        }

        void HandleMessageFailedMaxNumberOfTimes(TransportMessage transportMessage)
        {
            sendMessages.Send(@".\private$\error", transportMessage);
        }

        string GetReturnAddress()
        {
            return MessageContext.GetCurrent().ReturnAddressOfCurrentTransportMessage;
        }
    }
}