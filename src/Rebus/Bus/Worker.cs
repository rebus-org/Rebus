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
using System.Collections.Concurrent;
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
    public class Worker : IDisposable
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

            workerThread = new Thread(MainLoop) { Name = GenerateNewWorkerThreadName() };
            workerThread.Start();

            Log.Info("Worker {0} created and inner thread started", WorkerThreadName);
        }

        /// <summary>
        /// Event that will be raised whenever dispatching a given message has failed MAX number of times
        /// (usually 5 or something like that).
        /// </summary>
        public event Action<ReceivedTransportMessage, string> MessageFailedMaxNumberOfTimes = delegate { };

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

        public string WorkerThreadName
        {
            get { return workerThread.Name; }
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
                    MessageFailedMaxNumberOfTimes(transportMessage, errorTracker.GetErrorText(id));
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
                                var typeToDispatch = logicalMessage.GetType();

                                Log.Debug("Dispatching message {0}: {1}", id, typeToDispatch);

                                try
                                {
                                    GetDispatchMethod(typeToDispatch).Invoke(this, new[] {logicalMessage});
                                }
                                catch(TargetInvocationException tae)
                                {
                                    throw tae.InnerException;
                                }
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Log.Error(exception, "Handling message {0} has failed", id);
                        errorTracker.Track(id, exception);
                        throw;
                    }
                }

                transactionScope.Complete();
                errorTracker.Forget(id);
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

        /// <summary>
        /// Private strongly typed dispatcher method. Will be invoked through reflection to allow
        /// for some strongly typed interaction from this point and on....
        /// </summary>
        internal void DispatchGeneric<T>(T message)
        {
            dispatcher.Dispatch(message);
        }

        string GenerateNewWorkerThreadName()
        {
            return string.Format("Rebus worker #{0}", Interlocked.Increment(ref workerThreadCounter));
        }
    }
}