using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Rebus
{
    public class RebusBus
    {
        readonly ISendMessages sendMessages;
        readonly IReceiveMessages receiveMessages;
        readonly IStoreSubscriptions storeSubscriptions;
        readonly IHandlerFactory handlerFactory;
        readonly List<Worker> workers = new List<Worker>();

        public RebusBus(IHandlerFactory handlerFactory, ISendMessages sendMessages, IReceiveMessages receiveMessages, IStoreSubscriptions storeSubscriptions)
        {
            this.handlerFactory = handlerFactory;
            this.sendMessages = sendMessages;
            this.receiveMessages = receiveMessages;
            this.storeSubscriptions = storeSubscriptions;
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

        public void Send(string endpoint, object message)
        {
            sendMessages.Send(endpoint, new TransportMessage
                                            {
                                                Messages = new[] { message },
                                                ReturnAddress = receiveMessages.InputQueue,
                                            });
        }

        public void Reply(object message)
        {
            sendMessages.Send(GetReturnAddress(), new TransportMessage
                                                      {
                                                          Messages = new[] { message },
                                                          ReturnAddress = receiveMessages.InputQueue,
                                                      });
        }

        string GetReturnAddress()
        {
            return MessageContext.GetCurrent().ReturnAddressOfCurrentTransportMessage;
        }

        class MessageContext : IDisposable
        {
            [ThreadStatic]
            static MessageContext current;

            public MessageContext()
            {
                current = this;
            }

            public string ReturnAddressOfCurrentTransportMessage { get; set; }

            public static MessageContext GetCurrent()
            {
                if (current == null)
                {
                    throw new InvalidOperationException("No message context available - the MessageContext instance will only be set during the handling of messages, and it is available only on the worker thread.");
                }

                return current;
            }

            public void Dispose()
            {
                current = null;
            }
        }

        class Worker : IDisposable
        {
            readonly Thread workerThread;
            readonly IReceiveMessages receiveMessages;
            readonly IHandlerFactory handlerFactory;
            readonly IStoreSubscriptions storeSubscriptions;

            volatile bool shouldExit;
            volatile bool shouldWork;

            public Worker(IReceiveMessages receiveMessages, IHandlerFactory handlerFactory, IStoreSubscriptions storeSubscriptions)
            {
                this.receiveMessages = receiveMessages;
                this.handlerFactory = handlerFactory;
                this.storeSubscriptions = storeSubscriptions;
                workerThread = new Thread(DoWork);
                workerThread.Start();
            }

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

            /// <summary>
            /// Private strongly typed dispatcher method
            /// </summary>
            void Dispatch<T>(T message)
            {
                IHandleMessages<T>[] handlers = null;

                try
                {
                    handlers = handlerFactory
                        .GetHandlerInstancesFor<T>()
                        .ToArray();

                    foreach (var handler in handlers.Concat(OwnHandlersFor<T>()))
                    {
                        handler.Handle(message);
                    }
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
                finally
                {
                    if (handlers != null)
                    {
                        handlerFactory.ReleaseHandlerInstances(handlers);
                    }
                }
            }

            IEnumerable<IHandleMessages<T>> OwnHandlersFor<T>()
            {
                if (typeof(T) == typeof(SubscriptionMessage))
                {
                    return new[] {(IHandleMessages<T>) new SubHandler(storeSubscriptions)};
                }

                return new IHandleMessages<T>[0];
            }

            class SubHandler : IHandleMessages<SubscriptionMessage>
            {
                IStoreSubscriptions storeSubscriptions;

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

            void DoWork()
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
                        var transportMessage = receiveMessages.ReceiveMessage();

                        if (transportMessage == null) continue;

                        var messageContext = new MessageContext
                                                 {
                                                     ReturnAddressOfCurrentTransportMessage =
                                                         transportMessage.ReturnAddress
                                                 };

                        using (messageContext)
                        {
                            foreach (var message in transportMessage.Messages)
                            {
                                GetType().GetMethod("Dispatch", BindingFlags.Instance | BindingFlags.NonPublic)
                                    .MakeGenericMethod(message.GetType())
                                    .Invoke(this, new[] {message});
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
        }

        void AddWorker()
        {
            var worker = new Worker(receiveMessages, handlerFactory, storeSubscriptions);
            workers.Add(worker);
            worker.Start();
        }

        public void Subscribe<TMessage>(string publisherInputQueue)
        {
            sendMessages.Send(publisherInputQueue,
                              new TransportMessage
                                  {
                                      ReturnAddress = receiveMessages.InputQueue,
                                      Messages = new object[]
                                                     {
                                                         new SubscriptionMessage
                                                             {
                                                                 Type = typeof (TMessage).FullName,
                                                             }
                                                     }
                                  });
        }

        public void Publish(object message)
        {
            foreach(var subscriberInputQueue in storeSubscriptions.GetSubscribers(message.GetType()))
            {
                Send(subscriberInputQueue, message);
            }
        }
    }
}