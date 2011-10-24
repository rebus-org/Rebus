using System;
using System.Collections.Concurrent;
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

        /// <summary>
        /// Caching of dispatcher methods
        /// </summary>
        static readonly ConcurrentDictionary<Type, MethodInfo> DispatchMethodCache = new ConcurrentDictionary<Type, MethodInfo>();
        
        /// <summary>
        /// Caching of polymorphic types to attempt to dispatch, given the type of an incoming message
        /// </summary>
        static readonly ConcurrentDictionary<Type, Type[]> TypesToDispatchCache = new ConcurrentDictionary<Type, Type[]>();

        /// <summary>
        /// Keeps count of worker thread IDs.
        /// </summary>
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
            workerThread = new Thread(MainLoop) {Name = GenerateNewWorkerThreadName()};
            workerThread.Start();
            Log.InfoFormat("Worker {0} created and inner thread started", WorkerThreadName);
        }

        /// <summary>
        /// Event that will be raised whenever dispatching a given message has failed MAX number of times
        /// (usually 5 or something like that).
        /// </summary>
        public event Action<TransportMessage> MessageFailedMaxNumberOfTimes = delegate { };
        
        /// <summary>
        /// Event that will be raised in the unlikely event that something outside of the usual
        /// message dispatch goes wrong.
        /// </summary>
        public event Action<Worker, Exception> UnhandledException = delegate { }; 

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
                    UnhandledException(this, e);
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
                    Log.ErrorFormat("Handling message {0} has failed the maximum number of times", id);
                    MessageFailedMaxNumberOfTimes(transportMessage);
                    errorTracker.Forget(id);
                }
                else
                {
                    try
                    {
                        var message = serializeMessages.Deserialize(transportMessage);

                        var returnAddress = message.GetHeader(Headers.ReturnAddress);

                        using (MessageContext.Enter(returnAddress))
                        {
                            foreach (var logicalMessage in message.Messages)
                            {
                                Log.DebugFormat("Dispatching message {0}: {1}", id, logicalMessage.GetType());
                                Dispatch(logicalMessage);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Log.Error(string.Format("Handling message {0} has failed", id), exception);
                        errorTracker.Track(id, exception);
                        throw;
                    }
                }

                transactionScope.Complete();
                errorTracker.Forget(id);
            }
        }

        void Dispatch(object message)
        {
            var messageType = message.GetType();

            try
            {
                foreach (var typeToDispatch in GetTypesToDispatch(messageType))
                {
                    GetDispatchMethod(typeToDispatch).Invoke(this, new[] { message });
                }
            }
            catch (TargetInvocationException tae)
            {
                throw tae.InnerException;
            }
        }

        MethodInfo GetDispatchMethod(Type typeToDispatch)
        {
            MethodInfo method;
            if (DispatchMethodCache.TryGetValue(typeToDispatch, out method))
            {
                return method;
            }

            var newMethod = GetType()
                .GetMethod("DispatchGeneric", BindingFlags.Instance | BindingFlags.NonPublic)
                .MakeGenericMethod(typeToDispatch);

            DispatchMethodCache.TryAdd(typeToDispatch, newMethod);

            return newMethod;
        }

        Type[] GetTypesToDispatch(Type messageType)
        {
            Type[] typesToDispatch;

            if (TypesToDispatchCache.TryGetValue(messageType, out typesToDispatch))
            {
                return typesToDispatch;
            }

            var types = new HashSet<Type>();
            AddTypesFrom(messageType, types);
            var newArrayOfTypesToDispatch = types.ToArray();

            TypesToDispatchCache.TryAdd(messageType, newArrayOfTypesToDispatch);

            return newArrayOfTypesToDispatch;
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

        public string WorkerThreadName
        {
            get { return workerThread.Name; }
        }

        string GenerateNewWorkerThreadName()
        {
            return string.Format("Rebus worker #{0}", Interlocked.Increment(ref workerThreadCounter));
        }

        /// <summary>
        /// Private strongly typed dispatcher method. Will be invoked through reflection to allow
        /// for some strongly typed interaction inside of this method.
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