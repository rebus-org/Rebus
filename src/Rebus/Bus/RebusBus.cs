// Copyright 2011 Mogens Heller Grabe
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
using System;
using System.Collections.Generic;
using System.Reflection;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;

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

        public void Send<TMessage>(TMessage message)
        {
            var destinationEndpoint = GetMessageOwnerEndpointFor(message.GetType());

            Send(destinationEndpoint, message);
        }

        public void Send<TMessage>(string endpoint, TMessage message)
        {
            var messageToSend = new Message
                                    {
                                        Messages = new object[] {message},
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
            Log.Error(exception, string.Format("Unhandled exception in worker {0}. This is not good.", worker.WorkerThreadName));
        }

        void HandleMessageFailedMaxNumberOfTimes(ReceivedTransportMessage transportMessage, string errorDetail)
        {
            var transportMessageToSend = transportMessage.ToForwardableMessage();

            sendMessages.Send(@".\private$\error", transportMessageToSend);
        }
    }
}