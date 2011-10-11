using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Transactions;
using Rebus.Extensions;
using Rebus.Messages;

namespace Rebus.Bus
{
    public class RebusBus : IBus
    {
        readonly ISendMessages sendMessages;
        readonly IReceiveMessages receiveMessages;
        readonly IStoreSubscriptions storeSubscriptions;
        readonly IDetermineDestination determineDestination;
        readonly IActivateHandlers activateHandlers;
        readonly List<Worker> workers = new List<Worker>();
        readonly ErrorTracker errorTracker = new ErrorTracker();

        public RebusBus(IActivateHandlers activateHandlers,
            ISendMessages sendMessages,
            IReceiveMessages receiveMessages,
            IStoreSubscriptions storeSubscriptions,
            IDetermineDestination determineDestination)
        {
            this.activateHandlers = activateHandlers;
            this.sendMessages = sendMessages;
            this.receiveMessages = receiveMessages;
            this.storeSubscriptions = storeSubscriptions;
            this.determineDestination = determineDestination;
        }

        public RebusBus Start()
        {
            return Start(1);
        }

        public RebusBus Start(int numberOfWorkers)
        {
            numberOfWorkers.Times(AddWorker);
            return this;
        }

        public void Send(object message)
        {
            Send(determineDestination.GetEndpointFor(message.GetType()), message);
        }

        public void Send(string endpoint, object message)
        {
            sendMessages.Send(endpoint, new TransportMessage
                                            {
                                                Messages = new[] {message},
                                                Headers = {{Headers.ReturnAddress, receiveMessages.InputQueue}}
                                            });
        }

        public void Publish(object message)
        {
            foreach (var subscriberInputQueue in storeSubscriptions.GetSubscribers(message.GetType()))
            {
                Send(subscriberInputQueue, message);
            }
        }

        public void Reply(object message)
        {
            sendMessages.Send(GetReturnAddress(),
                              new TransportMessage
                                  {
                                      Messages = new[] {message},
                                      Headers = {{Headers.ReturnAddress, receiveMessages.InputQueue}}
                                  });
        }

        public void Subscribe<TMessage>()
        {
            Subscribe<TMessage>(determineDestination.GetEndpointFor(typeof(TMessage)));
        }

        public void Subscribe<TMessage>(string publisherInputQueue)
        {
            sendMessages.Send(publisherInputQueue,
                              new TransportMessage
                                  {
                                      Messages = new object[]
                                                     {
                                                         new SubscriptionMessage
                                                             {
                                                                 Type = typeof (TMessage).FullName,
                                                             }
                                                     },
                                      Headers = {{Headers.ReturnAddress, receiveMessages.InputQueue}}
                                  });
        }

        public void Dispose()
        {
            workers.ForEach(w => w.Stop());
            workers.ForEach(w => w.Dispose());
        }

        class Worker : IDisposable
        {
            readonly Thread workerThread;
            readonly IReceiveMessages receiveMessages;
            readonly IActivateHandlers activateHandlers;
            readonly IStoreSubscriptions storeSubscriptions;
            readonly ErrorTracker errorTracker;

            volatile bool shouldExit;
            volatile bool shouldWork;

            public Worker(IReceiveMessages receiveMessages, IActivateHandlers activateHandlers, IStoreSubscriptions storeSubscriptions, ErrorTracker errorTracker)
            {
                this.receiveMessages = receiveMessages;
                this.activateHandlers = activateHandlers;
                this.storeSubscriptions = storeSubscriptions;
                this.errorTracker = errorTracker;
                workerThread = new Thread(MainLoop);
                workerThread.Start();
            }

            public event Action<TransportMessage> MessageFailedMaxNumberOfTimes = delegate { };

            public void Start()
            {
                shouldWork = true;
            }

            public void Pause()
            {
                shouldWork = false;
            }

            public void Stop()
            {
                shouldWork = false;
                shouldExit = true;
            }

            public void Dispose()
            {
                Stop();

                if (!workerThread.Join(TimeSpan.FromSeconds(30)))
                {
                    workerThread.Abort();
                }
            }

            IEnumerable<IHandleMessages<T>> OwnHandlersFor<T>()
            {
                if (typeof(T) == typeof(SubscriptionMessage))
                {
                    return new[] { (IHandleMessages<T>)new SubHandler(storeSubscriptions) };
                }

                return new IHandleMessages<T>[0];
            }

            class SubHandler : IHandleMessages<SubscriptionMessage>
            {
                readonly IStoreSubscriptions storeSubscriptions;

                public SubHandler(IStoreSubscriptions storeSubscriptions)
                {
                    this.storeSubscriptions = storeSubscriptions;
                }

                public void Handle(SubscriptionMessage message)
                {
                    var subscriberInputQueue = MessageContext.GetCurrent().ReturnAddressOfCurrentTransportMessage;

                    storeSubscriptions.Save(Type.GetType(message.Type), subscriberInputQueue);
                }
            }

            void MainLoop()
            {
                while (!shouldExit)
                {
                    if (!shouldWork)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    try
                    {
                        TryProcessIncomingMessage();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }

            void TryProcessIncomingMessage()
            {
                using (var transactionScope = new TransactionScope())
                {
                    var transportMessage = receiveMessages.ReceiveMessage();

                    if (transportMessage == null) return;

                    var id = transportMessage.Id;

                    if (errorTracker.MessageHasFailedMaximumNumberOfTimes(id))
                    {
                        transportMessage.SetHeader(Headers.ErrorMessage, errorTracker.GetErrorText(id));
                        MessageFailedMaxNumberOfTimes(transportMessage);
                        errorTracker.SignOff(id);
                    }
                    else
                    {
                        try
                        {
                            using (MessageContext.Enter(transportMessage.GetHeader(Headers.ReturnAddress)))
                            {
                                foreach (var message in transportMessage.Messages)
                                {
                                    Dispatch(message);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            errorTracker.TrackError(id, e);
                            throw;
                        }
                    }

                    transactionScope.Complete();
                    errorTracker.SignOff(id);
                }
            }

            void Dispatch(object message)
            {
                var messageType = message.GetType();

                try
                {
                    foreach (var typeToDispatch in GetTypesToDispatch(messageType))
                    {
                        GetType().GetMethod("DispatchGeneric", BindingFlags.Instance | BindingFlags.NonPublic)
                            .MakeGenericMethod(typeToDispatch)
                            .Invoke(this, new[] { message });
                    }
                }
                catch (TargetInvocationException tae)
                {
                    throw tae.InnerException;
                }
            }

            /// <summary>
            /// TODO perf: cache types to dispatch - no need to dribble recursively every time
            /// </summary>
            Type[] GetTypesToDispatch(Type messageType)
            {
                var types = new HashSet<Type>();
                AddTypesFrom(messageType, types);
                return types.ToArray();
            }

            void AddTypesFrom(Type messageType, HashSet<Type> typeSet)
            {
                typeSet.Add(messageType);
                foreach (var interfaceType in messageType.GetInterfaces())
                {
                    typeSet.Add(interfaceType);
                }
                if (messageType.BaseType != null)
                {
                    AddTypesFrom(messageType.BaseType, typeSet);
                }
            }

            /// <summary>
            /// Private strongly typed dispatcher method
            /// </summary>
            void DispatchGeneric<T>(T message)
            {
                IHandleMessages<T>[] handlers = null;

                try
                {
                    var handlerInstances = activateHandlers.GetHandlerInstancesFor<T>();

                    // if we didn't get anything, just carry on... might not be what we want, but let's just do that for now
                    if (handlerInstances == null) return;

                    handlers = handlerInstances.ToArray();

                    foreach (var handler in handlers.Concat(OwnHandlersFor<T>()))
                    {
                        handler.Handle(message);
                    }
                }
                finally
                {
                    if (handlers != null)
                    {
                        activateHandlers.ReleaseHandlerInstances(handlers);
                    }
                }
            }
        }

        void AddWorker()
        {
            var worker = new Worker(receiveMessages, activateHandlers, storeSubscriptions, errorTracker);
            workers.Add(worker);
            worker.MessageFailedMaxNumberOfTimes += HandleMessageFailedMaxNumberOfTimes;
            worker.Start();
        }

        void HandleMessageFailedMaxNumberOfTimes(TransportMessage message)
        {
            sendMessages.Send(@".\private$\error", message);
        }

        string GetReturnAddress()
        {
            return MessageContext.GetCurrent().ReturnAddressOfCurrentTransportMessage;
        }
    }
}