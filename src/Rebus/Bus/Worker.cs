using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Transactions;
using Rebus.Messages;
using log4net;

namespace Rebus.Bus
{
    /// <summary>
    /// Internal worker thread that continually attempts to receive messages and dispatch to handlers.
    /// </summary>
    class Worker : IDisposable
    {
        static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static int workerThreadCounter = 0;

        readonly Thread workerThread;
        readonly ErrorTracker errorTracker;
        readonly IReceiveMessages receiveMessages;
        readonly IActivateHandlers activateHandlers;
        readonly IStoreSubscriptions storeSubscriptions;
        readonly ISerializeMessages serializeMessages;

        volatile bool shouldExit;
        volatile bool shouldWork;

        public Worker(ErrorTracker errorTracker, 
                      IReceiveMessages receiveMessages, 
                      IActivateHandlers activateHandlers, 
                      IStoreSubscriptions storeSubscriptions,
                      ISerializeMessages serializeMessages)
        {
            this.receiveMessages = receiveMessages;
            this.activateHandlers = activateHandlers;
            this.storeSubscriptions = storeSubscriptions;
            this.serializeMessages = serializeMessages;
            this.errorTracker = errorTracker;
            workerThread = new Thread(MainLoop);
            workerThread.Name = GenerateNewWorkerThreadName();
            workerThread.Start();
            Log.InfoFormat("Worker {0} created and inner thread started", WorkerThreadName);
        }

        string WorkerThreadName
        {
            get { return workerThread.Name; }
        }

        string GenerateNewWorkerThreadName()
        {
            return string.Format("Rebus worker #{0}", Interlocked.Increment(ref workerThreadCounter));
        }

        public event Action<TransportMessage> MessageFailedMaxNumberOfTimes = delegate { };

        public void Start()
        {
            Log.InfoFormat("Starting worker thread {0}", WorkerThreadName);
            shouldWork = true;
        }

        public void Pause()
        {
            Log.InfoFormat("Pausing worker thread {0}", WorkerThreadName);
            shouldWork = false;
        }

        public void Stop()
        {
            Log.InfoFormat("Stopping worker thread {0}", WorkerThreadName);
            shouldWork = false;
            shouldExit = true;
        }

        public void Dispose()
        {
            Log.InfoFormat("Disposing worker thread {0}", WorkerThreadName);

            if (shouldWork)
            {
                Log.InfoFormat("Worker thread {0} is currently working", WorkerThreadName);
                Stop();
            }

            if (!workerThread.Join(TimeSpan.FromSeconds(30)))
            {
                Log.InfoFormat("Worker thread {0} did not exit within 30 seconds - aborting!", WorkerThreadName);
                workerThread.Abort();
            }
        }

        IEnumerable<IHandleMessages<T>> OwnHandlersFor<T>()
        {
            if (typeof(T) == typeof(SubscriptionMessage))
            {
                return new[] { (IHandleMessages<T>)new SubscriptionMessageHandler(storeSubscriptions) };
            }

            return new IHandleMessages<T>[0];
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
                    MessageFailedMaxNumberOfTimes(transportMessage);
                    errorTracker.SignOff(id);
                }
                else
                {
                    try
                    {
                        var message = serializeMessages.Deserialize(transportMessage);
                            
                        using (MessageContext.Enter(message.GetHeader(Headers.ReturnAddress)))
                        {
                            foreach (var logicalMessage in message.Messages)
                            {
                                Dispatch(logicalMessage);
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
}