using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Transactions;
using Rebus.Logging;
using Rebus.Messages;

namespace Rebus.Bus
{
    /// <summary>
    /// Internal worker thread that continually attempts to receive messages and dispatch to handlers.
    /// </summary>
    class Worker : IDisposable
    {
        static readonly ILog Log = RebusLoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Caching of dispatcher methods
        /// </summary>
        readonly ConcurrentDictionary<Type, MethodInfo> dispatchMethodCache = new ConcurrentDictionary<Type, MethodInfo>();
        
        /// <summary>
        /// Caching of polymorphic types to attempt to dispatch, given the type of an incoming message
        /// </summary>
        readonly ConcurrentDictionary<Type, Type[]> typesToDispatchCache = new ConcurrentDictionary<Type, Type[]>();

        /// <summary>
        /// Keeps count of worker thread IDs.
        /// </summary>
        static int workerThreadCounter;

        readonly Thread workerThread;
        readonly Dispatcher dispatcher;
        readonly ErrorTracker errorTracker;
        readonly IReceiveMessages receiveMessages;
        readonly ISerializeMessages serializeMessages;

        volatile bool shouldExit;
        volatile bool shouldWork;

        public Worker(ErrorTracker errorTracker,
            IReceiveMessages receiveMessages,
            IActivateHandlers activateHandlers,
            IStoreSubscriptions storeSubscriptions,
            ISerializeMessages serializeMessages,
            IStoreSagaData storeSagaData,
            IInspectHandlerPipeline inspectHandlerPipeline)
        {
            this.receiveMessages = receiveMessages;
            this.serializeMessages = serializeMessages;
            this.errorTracker = errorTracker;
            dispatcher = new Dispatcher(storeSagaData, activateHandlers, storeSubscriptions, inspectHandlerPipeline);
            
            workerThread = new Thread(MainLoop) {Name = GenerateNewWorkerThreadName()};
            workerThread.Start();
            
            Log.Info("Worker {0} created and inner thread started", WorkerThreadName);
        }

        /// <summary>
        /// Event that will be raised whenever dispatching a given message has failed MAX number of times
        /// (usually 5 or something like that).
        /// </summary>
        public event Action<ReceivedTransportMessage> MessageFailedMaxNumberOfTimes = delegate { };
        
        /// <summary>
        /// Event that will be raised in the unlikely event that something outside of the usual
        /// message dispatch goes wrong.
        /// </summary>
        public event Action<Worker, Exception> UnhandledException = delegate { }; 

        public void Start()
        {
            Log.Info("Starting worker thread {0}", WorkerThreadName);
            shouldWork = true;
        }

        public void Pause()
        {
            Log.Info("Pausing worker thread {0}", WorkerThreadName);
            shouldWork = false;
        }

        public void Stop()
        {
            Log.Info("Stopping worker thread {0}", WorkerThreadName);
            shouldWork = false;
            shouldExit = true;
        }

        public void Dispose()
        {
            Log.Info("Disposing worker thread {0}", WorkerThreadName);

            if (shouldWork)
            {
                Log.Info("Worker thread {0} is currently working", WorkerThreadName);
                Stop();
            }

            if (!workerThread.Join(TimeSpan.FromSeconds(30)))
            {
                Log.Info("Worker thread {0} did not exit within 30 seconds - aborting!", WorkerThreadName);
                workerThread.Abort();
            }
        }

        void MainLoop()
        {
            while (!shouldExit)
            {
                if (!shouldWork)
                {
                    Thread.Sleep(20);
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
                
                if (transportMessage == null)
                {
                    Thread.Sleep(20);
                    return;
                }

                var id = transportMessage.Id;

                if (errorTracker.MessageHasFailedMaximumNumberOfTimes(id))
                {
                    Log.Error("Handling message {0} has failed the maximum number of times", id);
                    Console.WriteLine("FAIL!");
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
                                Log.Debug("Dispatching message {0}: {1}", id, logicalMessage.GetType());
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
            if (dispatchMethodCache.TryGetValue(typeToDispatch, out method))
            {
                return method;
            }

            var newMethod = GetType()
                .GetMethod("DispatchGeneric", BindingFlags.Instance | BindingFlags.NonPublic)
                .MakeGenericMethod(typeToDispatch);

            dispatchMethodCache.TryAdd(typeToDispatch, newMethod);

            return newMethod;
        }

        Type[] GetTypesToDispatch(Type messageType)
        {
            Type[] typesToDispatch;

            if (typesToDispatchCache.TryGetValue(messageType, out typesToDispatch))
            {
                return typesToDispatch;
            }

            var types = new HashSet<Type>();
            AddTypesFrom(messageType, types);
            var newArrayOfTypesToDispatch = types.ToArray();

            typesToDispatchCache.TryAdd(messageType, newArrayOfTypesToDispatch);

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
        /// for some strongly typed interaction from this point and on....
        /// </summary>
        internal void DispatchGeneric<T>(T message)
        {
            dispatcher.Dispatch(message);
        }
    }
}