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
            AddWorker();
            return this;
        }

        public void Send(string endpoint, object message)
        {
            sendMessages.Send(endpoint, message);
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

                    object handlerInstance = null;

                    try
                    {
                        var message = receiveMessages.ReceiveMessage();

                        if (message == null) continue;

                        GetType().GetMethod("Dispatch", BindingFlags.Instance | BindingFlags.NonPublic)
                            .MakeGenericMethod(message.GetType())
                            .Invoke(this, new[] { message });
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