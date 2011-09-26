using System;
using System.Collections.Generic;
using System.Threading;

namespace Rebus
{
    public class RebusBus
    {
        readonly IMessageQueue messageQueue;
        readonly IHandlerFactory handlerFactory;
        readonly List<Worker> workers = new List<Worker>();

        public RebusBus(IMessageQueue messageQueue, IHandlerFactory handlerFactory)
        {
            this.messageQueue = messageQueue;
            this.handlerFactory = handlerFactory;
        }

        public void Start()
        {
            AddWorker();
        }

        public void Send(string endpoint, object message)
        {
            messageQueue.Send(endpoint, message);
        }

        class Worker : IDisposable
        {
            readonly Thread workerThread;
            readonly IMessageQueue messageQueue;
            readonly IHandlerFactory handlerFactory;

            volatile bool shouldExit;
            volatile bool shouldWork;

            public Worker(IMessageQueue messageQueue, IHandlerFactory handlerFactory)
            {
                this.messageQueue = messageQueue;
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
                        var message = messageQueue.ReceiveMessage();

                        if (message == null) continue;

                        handlerInstance = handlerFactory.GetType()
                            .GetMethod("GetHandlerInstanceFor")
                            .MakeGenericMethod(message.GetType())
                            .Invoke(handlerFactory, new object[0]);

                        handlerInstance.GetType()
                            .GetMethod("Handle")
                            .Invoke(handlerInstance, new[] { message });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    finally
                    {
                        if (handlerInstance != null)
                        {
                            //handlerFactory.GetType()
                            //    .GetMethod("ReleaseHandlerInstance")
                            //    .MakeGenericMethod(handlerInstance.GetType())
                            //    .Invoke(handlerFactory, new[] { handlerInstance });
                        }
                    }
                }
            }
        }

        void AddWorker()
        {
            var worker = new Worker(messageQueue, handlerFactory);
            workers.Add(worker);
            worker.Start();
        }
    }

    public interface IHandlerFactory
    {
        IHandleMessages<T> GetHandlerInstanceFor<T>();
        void ReleaseHandlerInstance<T>(IHandleMessages<T> handlerInstance);
    }
}