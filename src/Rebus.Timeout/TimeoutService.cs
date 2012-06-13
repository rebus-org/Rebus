using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using System.Transactions;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Serialization.Json;
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

        public const string InputQueueName = "rebus.timeout";

        IAdvancedBus bus;
        readonly Timer timer = new Timer();
        RebusBus rebusBus;
        static readonly Type[] IgnoredMessageTypes = new[] { typeof(object), typeof(IRebusControlMessage) };

        public TimeoutService(IStoreTimeouts storeTimeouts)
        {
            var msmqMessageQueue = new MsmqMessageQueue(InputQueueName);
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

        public string InputQueue
        {
            get { return InputQueueName; }
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

            var newTimeout = new Timeout
                                 {
                                     SagaId = message.SagaId,
                                     CorrelationId = message.CorrelationId,
                                     ReplyTo = currentMessageContext.ReturnAddress,
                                     TimeToReturn = Time.Now() + message.Timeout,
                                     CustomData = message.CustomData,
                                 };

            storeTimeouts.Add(newTimeout);

            log.Info("Added new timeout: {0}", newTimeout);
        }

        void CheckCallbacks(object sender, ElapsedEventArgs e)
        {
            using (var tx = new TransactionScope())
            {
                var dueTimeouts = storeTimeouts.RemoveDueTimeouts();

                foreach (var timeout in dueTimeouts)
                {
                    log.Info("Timeout!: {0} -> {1}", timeout.CorrelationId, timeout.ReplyTo);

                    bus.Send(timeout.ReplyTo,
                             new TimeoutReply
                                 {
                                     SagaId = timeout.SagaId,
                                     CorrelationId = timeout.CorrelationId,
                                     DueTime = timeout.TimeToReturn,
                                     CustomData = timeout.CustomData,
                                 });
                }

                tx.Complete();
            }
        }
    }
}