using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Rebus
{
    public class RebusBus
    {
        readonly ISendMessages sendMessages;
        readonly IReceiveMessages receiveMessages;
        readonly IHandlerFactory handlerFactory;
        readonly List<Worker> workers = new List<Worker>();

        public RebusBus(IHandlerFactory handlerFactory, ISendMessages sendMessages, IReceiveMessages receiveMessages)
        {
            this.handlerFactory = handlerFactory;
            this.sendMessages = sendMessages;
            this.receiveMessages = receiveMessages;
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

        class MessageContext
        {
            [ThreadStatic]
            static MessageContext current;

            public static MessageContext Current
            {
                set { current = value; }
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
        }

        class Worker : IDisposable
        {
            readonly Thread workerThread;
            readonly IReceiveMessages receiveMessages;
            readonly IHandlerFactory handlerFactory;

            volatile bool shouldExit;
            volatile bool shouldWork;

            public Worker(IReceiveMessages receiveMessages, IHandlerFactory handlerFactory)
            {
                this.receiveMessages = receiveMessages;
                this.handlerFactory = handlerFactory;
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

                    foreach (var handler in handlers)
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

                        MessageContext.Current = new MessageContext
                                                     {
                                                         ReturnAddressOfCurrentTransportMessage =
                                                             transportMessage.ReturnAddress
                                                     };

                        foreach (var message in transportMessage.Messages)
                        {
                            GetType().GetMethod("Dispatch", BindingFlags.Instance | BindingFlags.NonPublic)
                                .MakeGenericMethod(message.GetType())
                                .Invoke(this, new[] { message });
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    finally
                    {
                        MessageContext.Current = null;
                    }
                }
            }
        }

        void AddWorker()
        {
            var worker = new Worker(receiveMessages, handlerFactory);
            workers.Add(worker);
            worker.Start();
        }
    }

    public interface IHandlerFactory
    {
        IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>();
        void ReleaseHandlerInstances<T>(IEnumerable<IHandleMessages<T>> handlerInstances);
    }

    public interface IHandleMessages<T>
    {
        void Handle(T message);
    }
}