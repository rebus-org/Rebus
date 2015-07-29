using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Rebus.Configuration;
using Rebus.Logging;
using System.Linq;
using Rebus.Shared;
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
        /// Helps with waiting an appropriate amount of time when no message is received
        /// </summary>
        readonly BackoffHelper nullMessageReceivedBackoffHelper;

        /// <summary>
        /// Helps with waiting an appropriate amount of time when something is wrong that makes us unable to do
        /// useful work
        /// </summary>
        readonly BackoffHelper errorThatBlocksOurAbilityToDoUsefulWorkBackoffHelper =
            new BackoffHelper(new[]
                              {
                                  TimeSpan.FromSeconds(1),
                                  TimeSpan.FromSeconds(2),
                                  TimeSpan.FromSeconds(3),
                                  TimeSpan.FromSeconds(5),
                                  TimeSpan.FromSeconds(8),
                                  TimeSpan.FromSeconds(13),
                                  TimeSpan.FromSeconds(21),
                                  TimeSpan.FromSeconds(30),
                              })
            {
                LoggingDisabled = true
            };

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
        readonly IEnumerable<IUnitOfWorkManager> unitOfWorkManagers;
        readonly ConfigureAdditionalBehavior configureAdditionalBehavior;
        readonly MessageLogger messageLogger;
        readonly RebusSynchronizationContext continuations;

        internal event Action<ReceivedTransportMessage> BeforeTransportMessage = delegate { };

        internal event Action<Exception, ReceivedTransportMessage> AfterTransportMessage = delegate { };

        internal event Action<ReceivedTransportMessage, PoisonMessageInfo> PoisonMessage = delegate { };

        internal event Action<object> BeforeMessage = delegate { };

        internal event Action<Exception, object> AfterMessage = delegate { };

        internal event Action<object, Saga> UncorrelatedMessage = delegate { };

        internal event Action<object, IHandleMessages> AfterHandling = delegate { };

        internal event Action<Exception> OnHandlingError = delegate { };

        internal event Action<object, IHandleMessages> BeforeHandling = delegate { };

        internal event Action<IMessageContext> MessageContextEstablished = delegate { };

        volatile bool shouldExit;
        volatile bool shouldWork;

        public Worker(
            IErrorTracker errorTracker,
            IReceiveMessages receiveMessages,
            IActivateHandlers activateHandlers,
            IStoreSubscriptions storeSubscriptions,
            ISerializeMessages serializeMessages,
            IStoreSagaData storeSagaData,
            IInspectHandlerPipeline inspectHandlerPipeline,
            string workerThreadName,
            IHandleDeferredMessage handleDeferredMessage,
            IMutateIncomingMessages mutateIncomingMessages,
            IStoreTimeouts storeTimeouts,
            IEnumerable<IUnitOfWorkManager> unitOfWorkManagers,
            ConfigureAdditionalBehavior configureAdditionalBehavior,
            MessageLogger messageLogger,
            RebusSynchronizationContext continuations)
        {
            this.receiveMessages = receiveMessages;
            this.serializeMessages = serializeMessages;
            this.mutateIncomingMessages = mutateIncomingMessages;
            this.unitOfWorkManagers = unitOfWorkManagers;
            this.configureAdditionalBehavior = configureAdditionalBehavior;
            this.messageLogger = messageLogger;
            this.continuations = continuations;
            this.errorTracker = errorTracker;
            dispatcher = new Dispatcher(storeSagaData, activateHandlers, storeSubscriptions, inspectHandlerPipeline, handleDeferredMessage, storeTimeouts);
            dispatcher.UncorrelatedMessage += RaiseUncorrelatedMessage;
            dispatcher.AfterHandling += RaiseAfterHandling;
            dispatcher.BeforeHandling += RaiseBeforeHandling;
            dispatcher.OnHandlingError += RaiseOnHandlingError;
            nullMessageReceivedBackoffHelper = CreateBackoffHelper(configureAdditionalBehavior.BackoffBehavior);

            workerThread = new Thread(MainLoop) { Name = workerThreadName };
            workerThread.Start();

            log.Info("Worker {0} created and inner thread started", WorkerThreadName);
        }

        void RaiseUncorrelatedMessage(object message, Saga saga)
        {
            UncorrelatedMessage(message, saga);
        }

        void RaiseAfterHandling(object message, IHandleMessages handler)
        {
            AfterHandling(message, handler);
        }

        void RaiseOnHandlingError(Exception exception)
        {
            OnHandlingError(exception);
        }

        void RaiseBeforeHandling(object message, IHandleMessages handler)
        {
            BeforeHandling(message, handler);
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
            SynchronizationContext.SetSynchronizationContext(continuations);

            while (!shouldExit)
            {
                if (!shouldWork)
                {
                    Thread.Sleep(20);
                    continue;
                }

                try
                {
                    try
                    {
                        continuations.Run();

                        TryProcessIncomingMessage();

                        errorThatBlocksOurAbilityToDoUsefulWorkBackoffHelper.Reset();
                    }
                    catch (MessageHandleException exception)
                    {
                        UserException(this, exception);
                    }
                    catch (UnitOfWorkCommitException exception)
                    {
                        UserException(this, exception);
                    }
                    catch (QueueCommitException exception)
                    {
                        UserException(this, exception);

                        // when the queue cannot commit, we can't get the message into the error queue either - basically,
                        // there's no way we can do useful work if we end up in here, so we just procrastinate for a while
                        errorThatBlocksOurAbilityToDoUsefulWorkBackoffHelper
                            .Wait(waitTime => log.Warn(
                                "Caught an exception when interacting with the queue system - there's basically no meaningful" +
                                " work we can do as long as we can't successfully commit a queue transaction, so instead we" +
                                " wait and hope that the situation gets better... current wait time is {0}",
                                waitTime));
                    }
                    catch (Exception e)
                    {
                        SystemException(this, e);
                    }
                }
                catch (Exception ex)
                {
                    errorThatBlocksOurAbilityToDoUsefulWorkBackoffHelper
                        .Wait(waitTime => log.Error(ex,
                            "An unhandled exception occurred while iterating the main worker loop - this is a critical error," +
                            " and there's a probability that we cannot do any useful work, which is why we'll wait for" +
                            " {0} and hope that the error goes away", waitTime));
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
        async void TryProcessIncomingMessage()
        {
            using (var context = new TxBomkarl())
            {
                try
                {
                    await DoTry();

                    try
                    {
                        context.RaiseDoCommit();
                    }
                    catch (Exception commitException)
                    {
                        throw new QueueCommitException(commitException);
                    }
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

        async Task DoTry()
        {
            var transportMessage = receiveMessages.ReceiveMessage(TransactionContext.Current);

            if (transportMessage == null)
            {
                // to back off and relax when there's no messages to process, we do this
                nullMessageReceivedBackoffHelper.Wait();
                return;
            }

            nullMessageReceivedBackoffHelper.Reset();

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

                // Populate rebus-msg-id, if not set, from transport-level-id
                if (!transportMessage.Headers.ContainsKey(Headers.MessageId))
                {
                    transportMessage.Headers[Headers.MessageId] = transportMessage.Id;
                }

                // Clean up Bounced header (to avoid problems with re-enqueued messages)
                if (transportMessage.Headers.ContainsKey(Headers.Bounced))
                {
                    transportMessage.Headers.Remove(Headers.Bounced);
                }

                using (var scope = BeginTransaction())
                {
                    var message = serializeMessages.Deserialize(transportMessage);
                    // successfully deserialized the transport message, let's enter a message context
                    context = MessageContext.Establish(message.Headers);
                    MessageContextEstablished(context);

                    var unitsOfWork = unitOfWorkManagers.Select(u => u.Create())
                                    .Where(u => !ReferenceEquals(null, u))
                                    .ToArray(); //< remember to invoke the chain here :)

                    try
                    {
                        foreach (var logicalMessage in message.Messages.Select(MutateIncoming))
                        {
                            context.SetLogicalMessage(logicalMessage);

                            Exception logicalMessageExceptionOrNull = null;
                            try
                            {
                                BeforeMessage(logicalMessage);

                                var typeToDispatch = logicalMessage.GetType();

                                messageLogger.LogReceive(id, logicalMessage);

                                try
                                {
                                    var dispatchMethod = GetDispatchMethod(typeToDispatch);
                                    var parameters = new[] { logicalMessage };
                                    await (Task)dispatchMethod.Invoke(dispatcher, parameters);
                                }
                                catch (TargetInvocationException tie)
                                {
                                    var exception = tie.InnerException;
                                    exception.PreserveStackTrace();
                                    throw exception;
                                }
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

                        foreach (var unitOfWork in unitsOfWork)
                        {
                            try
                            {
                                unitOfWork.Commit();
                            }
                            catch (Exception exception)
                            {
                                throw new UnitOfWorkCommitException(exception, unitOfWork);
                            }
                        }
                    }
                    catch
                    {
                        foreach (var unitOfWork in unitsOfWork)
                        {
                            try
                            {
                                unitOfWork.Abort();
                            }
                            catch (Exception abortException)
                            {
                                log.Warn("An error occurred while aborting the unit of work {0}: {1}",
                                         unitOfWork, abortException);
                            }
                        }
                        throw;
                    }
                    finally
                    {
                        foreach (var unitOfWork in unitsOfWork)
                        {
                            unitOfWork.Dispose();
                        }
                    }

                    if (scope != null)
                    {
                        scope.Complete();
                    }
                }
            }
            catch (Exception exception)
            {
                transportMessageExceptionOrNull = exception;
                log.Debug("Handling message {0} with ID {1} has failed", label, id);
                errorTracker.TrackDeliveryFail(id, exception);
                throw new MessageHandleException(id, exception);
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

                if (context != null)
                {
                    // dispose it if we entered
                    context.Dispose();
                }
            }

            errorTracker.StopTracking(id);
        }

        object MutateIncoming(object message)
        {
            return mutateIncomingMessages.MutateIncoming(message);
        }

        void HandlePoisonMessage(string id, ReceivedTransportMessage transportMessage)
        {
            var errorText = errorTracker.GetErrorText(id);
            log.Error("Handling message {0} has failed the maximum number of times - details: {1}", id, errorText);
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
            if (!configureAdditionalBehavior.HandleMessagesInTransactionScope)
            {
                return null;
            }

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

            var newMethod = dispatcher.GetType()
                .GetMethod("Dispatch", BindingFlags.Instance | BindingFlags.Public)
                .MakeGenericMethod(typeToDispatch);

            dispatchMethodCache.TryAdd(typeToDispatch, newMethod);

            return newMethod;
        }

        /// <summary>
        /// Create a backoff helper that matches the given behavior.
        /// </summary>
        static BackoffHelper CreateBackoffHelper(IEnumerable<TimeSpan> backoffTimes)
        {
            return new BackoffHelper(backoffTimes) { LoggingDisabled = true };
        }
    }
}