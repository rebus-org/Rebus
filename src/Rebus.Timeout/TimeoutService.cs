using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using System.Transactions;
using Rebus.Bus;
using Rebus.Log4Net;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Serialization.Json;
using Rebus.Transports.Msmq;
using log4net;
using System.Linq;
using ILog = log4net.ILog;

namespace Rebus.Timeout
{
    public class TimeoutService : IHandleMessages<TimeoutRequest>, IActivateHandlers
    {
        readonly IStoreTimeouts storeTimeouts;
        static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        const string InputQueueName = "rebus.timeout";
        
        readonly IBus bus;
        readonly Timer timer = new Timer();
        readonly RebusBus rebusBus;
        static readonly Type[] IgnoredMessageTypes = new[]{typeof(object), typeof(IRebusControlMessage)};

        public TimeoutService(IStoreTimeouts storeTimeouts)
        {
            this.storeTimeouts = storeTimeouts;
            var msmqMessageQueue = new MsmqMessageQueue(InputQueueName);

            RebusLoggerFactory.Current = new Log4NetLoggerFactory();
            rebusBus = new RebusBus(this, msmqMessageQueue, msmqMessageQueue, null, null, null, new JsonMessageSerializer(), new TrivialPipelineInspector());
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
                return new[] {(IHandleMessages<T>) this};
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
            Log.Info("Starting bus");
            rebusBus.Start(1);
            Log.Info("Starting inner timer");
            timer.Start();
        }

        public void Stop()
        {
            Log.Info("Stopping inner timer");
            timer.Stop();
            Log.Info("Disposing bus");
            rebusBus.Dispose();
        }

        public void Handle(TimeoutRequest message)
        {
            var currentMessageContext = MessageContext.GetCurrent();

            var newTimeout = new Timeout
                                 {
                                     CorrelationId = message.CorrelationId,
                                     ReplyTo = currentMessageContext.ReturnAddress,
                                     TimeToReturn = DateTime.UtcNow + message.Timeout,
                                 };

            storeTimeouts.Add(newTimeout);

            Log.InfoFormat("Added new timeout: {0}", newTimeout);
        }

        void CheckCallbacks(object sender, ElapsedEventArgs e)
        {
            using (var tx = new TransactionScope())
            {
                var dueTimeouts = storeTimeouts.RemoveDueTimeouts();

                foreach (var timeout in dueTimeouts)
                {
                    Log.InfoFormat("Timeout!: {0} -> {1}", timeout.CorrelationId, timeout.ReplyTo);

                    bus.Send(timeout.ReplyTo,
                             new TimeoutReply
                                 {
                                     CorrelationId = timeout.CorrelationId,
                                     DueTime = timeout.TimeToReturn
                                 });
                }

                tx.Complete();
            }
        }
    }
}