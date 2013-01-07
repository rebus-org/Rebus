using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Transactions;
using Rebus.Logging;
using System.Linq;
using Rebus.Timeout;

namespace Rebus.Bus
{
    /// <summary>
    /// Internal worker thread that continually attempts to receive messages and dispatch to handlers.
    /// </summary>
    class Worker : IDisposable
    {
        static ILog log;

        static Worker()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        /// <summary>
        /// Caching of dispatcher methods
        /// </summary>
        readonly ConcurrentDictionary<Type, MethodInfo> dispatchMethodCache = new ConcurrentDictionary<Type, MethodInfo>();

        readonly Thread workerThread;
        readonly Dispatcher dispatcher;
        readonly IErrorTracker errorTracker;
        readonly IReceiveMessages receiveMessages;
        readonly ISerializeMessages serializeMessages;
        readonly IMutateIncomingMessages mutateIncomingMessages;

        internal event Action<ReceivedTransportMessage> BeforeTransportMessage = delegate { };

        internal event Action<Exception, ReceivedTransportMessage> AfterTransportMessage = delegate { };

        internal event Action<ReceivedTransportMessage, PoisonMessageInfo> PoisonMessage = delegate { };

        internal event Action<object> BeforeMessage = delegate { };

        internal event Action<Exception, object> AfterMessage = delegate { };

        internal event Action<object, Saga> UncorrelatedMessage = delegate { };

        internal event Action<IMessageContext> MessageContextEstablished = delegate { }; 

        volatile bool shouldExit;
        volatile bool shouldWork;

        public Worker(IErrorTracker errorTracker,
            IReceiveMessages receiveMessages,
            IActivateHandlers activateHandlers,
            IStoreSubscriptions storeSubscriptions,
            ISerializeMessages serializeMessages,
            IStoreSagaData storeSagaData,
            IInspectHandlerPipeline inspectHandlerPipeline,
            string workerThreadName,
            IHandleDeferredMessage handleDeferredMessage,
            IMutateIncomingMessages mutateIncomingMessages,
            IStoreTimeouts storeTimeouts)
        {
            this.receiveMessages = receiveMessages;
            this.serializeMessages = serializeMessages;
            this.mutateIncomingMessages = mutateIncomingMessages;
            this.errorTracker = errorTracker;
            dispatcher = new Dispatcher(storeSagaData, activateHandlers, storeSubscriptions, inspectHandlerPipeline, handleDeferredMessage, storeTimeouts);
            dispatcher.UncorrelatedMessage += RaiseUncorrelatedMessage;

            workerThread = new Thread(MainLoop) { Name = workerThreadName };
            workerThread.Start();

            log.Info("Worker {0} created and inner thread started", WorkerThreadName);
        }

        void RaiseUncorrelatedMessage(object message, Saga saga)
        {
            UncorrelatedMessage(message, saga);
        }

        /// <summary>
        /// Event that will be raised whenever dispatching a given message has failed MAX number of times
        /// (usually 5 or something like that).
        /// </summary>
        public event Action<ReceivedTransportMessage, string> MessageFailedMaxNumberOfTimes = delegate { };

        /// <summary>
        /// Event that will be raised each time message delivery fails.
        /// </summary>
        public event Action<Worker, Exception> UserException = delegate { };

        /// <summary>
        /// Event that will be raised if an exception occurs outside of user code.
        /// </summary>
        public event Action<Worker, Exception> SystemException = delegate { };

        public void Start()
        {
            log.Info("Starting worker thread {0}", WorkerThreadName);
            shouldWork = true;
        }

        public void Pause()
        {
            log.Info("Pausing worker thread {0}", WorkerThreadName);
            shouldWork = false;
        }

        public void Stop()
        {
            if (shouldExit) return;

            log.Info("Stopping worker thread {0}", WorkerThreadName);
            shouldWork = false;
            shouldExit = true;
        }

        public void Dispose()
        {
            log.Info("Disposing worker thread {0}", WorkerThreadName);

            if (shouldWork)
            {
                log.Info("Worker thread {0} is currently working", WorkerThreadName);
                Stop();
            }

            if (!workerThread.Join(TimeSpan.FromSeconds(30)))
            {
                log.Info("Worker thread {0} did not exit within 30 seconds - aborting!", WorkerThreadName);
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
                    // if there's two levels of TargetInvocationExceptions, it's user code that threw...
                    if (e is TargetInvocationException && e.InnerException is TargetInvocationException)
                    {
                        UserException(this, e.InnerException.InnerException);
                    }
                    else if (e is TargetInvocationException)
                    {
                        UserException(this, e.InnerException);
                    }
                    else
                    {
                        SystemException(this, e);
                    }
                }
            }
        }

        /// <summary>
        /// OK - here's how stuff is nested:
        /// 
        /// - Message queue transaction (TxBomkarl)
        ///     - Before/After transport message
        ///         - TransactionScope
        ///             - Before/After logical message
        ///                 Dispatch logical message
        /// </summary>
        void TryProcessIncomingMessage()
        {
            using (var context = new TxBomkarl())
            {
                try
                {
                    DoTry();
                    context.RaiseDoCommit();
                }
                catch
                {
                    try
                    {
                        context.RaiseDoRollback();
                    }
                    catch (Exception e)
                    {
                        log.Error(e, "An error occurred while rolling back the transaction!");
                    }

                    throw;
                }
            }
        }

        void DoTry()
        {
            var transportMessage = receiveMessages.ReceiveMessage(TransactionContext.Current);

            if (transportMessage == null)
            {
                Thread.Sleep(20);
                return;
            }

            var id = transportMessage.Id;
            var label = transportMessage.Label;

            MessageContext context = null;

            if (id == null)
            {
                HandlePoisonMessage(id, transportMessage);
                return;
            }

            if (errorTracker.MessageHasFailedMaximumNumberOfTimes(id))
            {
                HandlePoisonMessage(id, transportMessage);
                errorTracker.StopTracking(id);
                return;
            }

            Exception transportMessageExceptionOrNull = null;
            try
            {
                BeforeTransportMessage(transportMessage);

                using (var scope = BeginTransaction())
                {
                    var message = serializeMessages.Deserialize(transportMessage);
                    // successfully deserialized the transport message, let's enter a message context
                    context = MessageContext.Establish(message.Headers);
                    MessageContextEstablished(context);

                    foreach (var logicalMessage in message.Messages.Select(MutateIncoming))
                    {
                        context.SetLogicalMessage(logicalMessage);

                        Exception logicalMessageExceptionOrNull = null;
                        try
                        {
                            BeforeMessage(logicalMessage);

                            var typeToDispatch = logicalMessage.GetType();

                            log.Debug("Dispatching message {0}: {1}", id, typeToDispatch);

                            GetDispatchMethod(typeToDispatch).Invoke(this, new[] { logicalMessage });
                        }
                        catch (Exception exception)
                        {
                            logicalMessageExceptionOrNull = exception;
                            throw;
                        }
                        finally
                        {
                            try
                            {
                                AfterMessage(logicalMessageExceptionOrNull, logicalMessage);
                            }
                            catch (Exception exceptionWhileRaisingEvent)
                            {
                                if (logicalMessageExceptionOrNull != null)
                                {
                                    log.Error(
                                        "An exception occurred while raising the AfterMessage event, and an exception occurred some" +
                                        " time before that as well. The first exception was this: {0}. And then, when raising the" +
                                        " AfterMessage event (including the details of the first error), this exception occurred: {1}",
                                        logicalMessageExceptionOrNull, exceptionWhileRaisingEvent);
                                }
                                else
                                {
                                    log.Error("An exception occurred while raising the AfterMessage event: {0}",
                                              exceptionWhileRaisingEvent);
                                }
                            }

                            context.ClearLogicalMessage();
                        }
                    }

                    scope.Complete();
                }
            }
            catch (Exception exception)
            {
                transportMessageExceptionOrNull = exception;
                log.Debug("Handling message {0} ({1}) has failed", label, id);
                errorTracker.TrackDeliveryFail(id, exception);
                throw;
            }
            finally
            {
                try
                {
                    AfterTransportMessage(transportMessageExceptionOrNull, transportMessage);
                }
                catch (Exception exceptionWhileRaisingEvent)
                {
                    if (transportMessageExceptionOrNull != null)
                    {
                        log.Error(
                            "An exception occurred while raising the AfterTransportMessage event, and an exception occurred some" +
                            " time before that as well. The first exception was this: {0}. And then, when raising the" +
                            " AfterTransportMessage event (including the details of the first error), this exception occurred: {1}",
                            transportMessageExceptionOrNull, exceptionWhileRaisingEvent);
                    }
                    else
                    {
                        log.Error("An exception occurred while raising the AfterTransportMessage event: {0}", exceptionWhileRaisingEvent);
                    }
                }

                if (context != null) context.Dispose(); //< dispose it if we entered
            }

            errorTracker.StopTracking(id);
        }

        object MutateIncoming(object message)
        {
            return mutateIncomingMessages.MutateIncoming(message);
        }

        void HandlePoisonMessage(string id, ReceivedTransportMessage transportMessage)
        {
            log.Error("Handling message {0} has failed the maximum number of times", id);
            var errorText = errorTracker.GetErrorText(id);
            var poisonMessageInfo = errorTracker.GetPoisonMessageInfo(id);

            MessageFailedMaxNumberOfTimes(transportMessage, errorText);
            errorTracker.StopTracking(id);

            try
            {
                PoisonMessage(transportMessage, poisonMessageInfo);
            }
            catch (Exception exceptionWhileRaisingEvent)
            {
                log.Error("An exception occurred while raising the PoisonMessage event: {0}",
                          exceptionWhileRaisingEvent);
            }
        }

        TransactionScope BeginTransaction()
        {
            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TransactionManager.DefaultTimeout
            };
            return new TransactionScope(TransactionScopeOption.Required, transactionOptions);
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
    }
}