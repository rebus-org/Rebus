using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Serialization.Json;
using Rebus.Shared;
using Rebus.Transports.Msmq;
using System.Linq;

namespace Rebus.Timeout
{
    public class TimeoutService : IHandleMessages<TimeoutRequest>, IActivateHandlers
    {
        static ILog log;

        static TimeoutService()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        IStoreTimeouts storeTimeouts;

        public const string DefaultInputQueueName = "rebus.timeout";

        IAdvancedBus bus;
        readonly Timer timer = new Timer();
        volatile bool currentlyChecking;
        readonly object checkLock = new object();

        RebusBus rebusBus;
        static readonly Type[] IgnoredMessageTypes = new[] { typeof(object), typeof(IRebusControlMessage) };

        public TimeoutService(IStoreTimeouts storeTimeouts)
        {
            var msmqMessageQueue = new MsmqMessageQueue(DefaultInputQueueName);
            Initialize(storeTimeouts, msmqMessageQueue, msmqMessageQueue);
        }

        public TimeoutService(IStoreTimeouts storeTimeouts, string inputQueueName)
        {
            var msmqMessageQueue = new MsmqMessageQueue(inputQueueName);
            Initialize(storeTimeouts, msmqMessageQueue, msmqMessageQueue);
        }

        public TimeoutService(IStoreTimeouts storeTimeouts, ISendMessages sendMessages, IReceiveMessages receiveMessages)
        {
            Initialize(storeTimeouts, sendMessages, receiveMessages);
        }

        void Initialize(IStoreTimeouts storeTimeouts, ISendMessages sendMessages, IReceiveMessages receiveMessages)
        {
            this.storeTimeouts = storeTimeouts;

            rebusBus = new RebusBus(this, sendMessages, receiveMessages, null, null, null, new JsonMessageSerializer(),
                                    new TrivialPipelineInspector(), new ErrorTracker(receiveMessages.InputQueueAddress + ".error"));
            bus = rebusBus;

            timer.Interval = 300;
            timer.Elapsed += CheckCallbacks;
        }

        public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
        {
            if (typeof(T) == typeof(TimeoutRequest))
            {
                return new[] { (IHandleMessages<T>)this };
            }

            if (IgnoredMessageTypes.Contains(typeof(T)))
            {
                return new IHandleMessages<T>[0];
            }

            throw new InvalidOperationException(string.Format("Someone took the chance and sent a message of type {0} to me.", typeof(T)));
        }

        public void Release(IEnumerable handlerInstances)
        {
        }

        public void Start()
        {
            log.Info("Starting bus");
            rebusBus.Start(1);
            log.Info("Starting inner timer");
            timer.Start();
        }

        public void Stop()
        {
            log.Info("Stopping inner timer");
            timer.Stop();
            log.Info("Disposing bus");
            rebusBus.Dispose();
        }

        public void Handle(TimeoutRequest message)
        {
            var currentMessageContext = MessageContext.GetCurrent();

            var newTimeout = new Timeout(currentMessageContext.ReturnAddress,
                                         message.CorrelationId,
                                         RebusTimeMachine.Now() + message.Timeout,
                                         message.SagaId,
                                         message.CustomData);

            storeTimeouts.Add(newTimeout);

            log.Info("Added new timeout: {0}", newTimeout);
        }

        void CheckCallbacks(object sender, ElapsedEventArgs e)
        {
            if (currentlyChecking) return;

            lock (checkLock)
            {
                if (currentlyChecking) return;

                try
                {
                    currentlyChecking = true;

                    foreach (var timeout in storeTimeouts.GetDueTimeouts())
                    {
                        log.Info("Timeout!: {0} -> {1}", timeout.CorrelationId, timeout.ReplyTo);

                        var sagaId = timeout.SagaId;

                        var reply = new TimeoutReply
                                        {
                                            SagaId = sagaId,
                                            CorrelationId = timeout.CorrelationId,
                                            DueTime = timeout.TimeToReturn,
                                            CustomData = timeout.CustomData,
                                        };

                        if (sagaId != Guid.Empty)
                        {
                            bus.AttachHeader(reply, Headers.AutoCorrelationSagaId, sagaId.ToString());
                        }

                        bus.Routing.Send(timeout.ReplyTo, reply);

                        timeout.MarkAsProcessed();
                    }
                }
                finally
                {
                    currentlyChecking = false;
                }
            }
        }
    }
}