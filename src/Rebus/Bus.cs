using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Messaging;
using System.Threading;

namespace Rebus
{
    public class Bus : IDisposable
    {
        readonly string path;
        readonly IHandlerBuilder handlerBuilder;
        readonly IQueue queue;
        readonly ConcurrentDictionary<string, Msmq.Sender> senders = new ConcurrentDictionary<string, Msmq.Sender>();
        readonly List<Worker> workers = new List<Worker>();

        [ThreadStatic]
        static MessageQueueTransaction tx;

        readonly MessageQueue messageQueue;

        public Bus(string path, IHandlerBuilder handlerBuilder, IProvideMessageTypes provideMessageTypes, IQueue queue)
        {
            this.path = path;
            this.handlerBuilder = handlerBuilder;
            this.queue = queue;

            messageQueue = MessageQueue.Exists(path)
                   ? new MessageQueue(path)
                   : MessageQueue.Create(path, transactional: true);

            messageQueue.Formatter = new XmlMessageFormatter(provideMessageTypes.GetMessageTypes());
        }

        public void Start()
        {
            if (workers.Count == 0)
            {
                SetNumberOfWorkers(1);
            }
        }

        public void Send<T>(string destinationQueue, T message)
        {
            Msmq.Sender sender;

            if (!senders.TryGetValue(destinationQueue, out sender))
            {
                lock (senders)
                {
                    if (!senders.TryGetValue(destinationQueue, out sender))
                        sender = new Msmq.Sender(messageQueue);

                    senders[destinationQueue] = sender;
                }
            }

            if (tx == null)
            {
                tx = new MessageQueueTransaction();
                tx.Begin();
            }

            sender.Send(message, tx);
        }

        public void Commit()
        {
            tx.Commit();
            tx = null;
        }

        public void RollBack()
        {
            tx.Abort();
            tx = null;
        }

        public void SetNumberOfWorkers(int numberOfWorkers)
        {
            if (numberOfWorkers < 0)
            {
                throw new InvalidOperationException(string.Format("Number of workers set to {0} - should be 0 or more", numberOfWorkers));
            }

            while (numberOfWorkers > workers.Count)
            {
                var worker = new Worker(handlerBuilder, messageQueue);
                workers.Add(worker);
                worker.Start();
            }

            while (numberOfWorkers < workers.Count)
            {
                for (var index = workers.Count - 1; index > numberOfWorkers; index--)
                {
                    workers[index].Stop();
                }
            }
        }

        public void Dispose()
        {
            workers.ForEach(w => w.Stop());
            workers.ForEach(w => w.Dispose());
        }
    }

    public interface IProvideMessageTypes
    {
        Type[] GetMessageTypes();
    }

    class Worker : IDisposable
    {
        readonly IHandlerBuilder handlerBuilder;
        readonly Msmq.Receiver receiver;
        readonly Thread workerThread;
        bool shouldDoWork;
        bool shouldExit;

        public Worker(IHandlerBuilder handlerBuilder, MessageQueue messageQueue)
        {
            this.handlerBuilder = handlerBuilder;
            workerThread = new Thread(DoWork);
            receiver = new Msmq.Receiver(messageQueue);
            workerThread.Start();
        }

        public void Start()
        {
            shouldDoWork = true;
        }

        public void Pause()
        {
            shouldDoWork = false;
        }

        public void Stop()
        {
            shouldDoWork = false;
            shouldExit = true;
        }

        void DoWork()
        {
            while (!shouldExit)
            {
                if (!shouldDoWork)
                {
                    Thread.Sleep(200);
                    continue;
                }

                receiver.Receive(MessageReceived);
            }
        }

        void MessageReceived(object message)
        {
            var handlerInstance = handlerBuilder.GetType()
                .GetMethod("GetHandlerInstanceFor")
                .MakeGenericMethod(message.GetType())
                .Invoke(handlerBuilder, new object[0]);

            handlerInstance.GetType()
                .GetMethod("Handle")
                .Invoke(handlerInstance, new[] { message });
        }

        public void Dispose()
        {
            Stop();

            if (!workerThread.Join(TimeSpan.FromSeconds(30)))
            {
                workerThread.Abort();
            }
        }
    }

    public interface IHandlerBuilder
    {
        IHandleMessages<T> GetHandlerInstanceFor<T>();
        void ReleaseHandlerInstance<T>(IHandleMessages<T> handler);
    }

    public interface IHandleMessages<T>
    {
        void Handle(T message);
    }
}