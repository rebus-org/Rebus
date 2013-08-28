using System;
using System.Timers;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Timeout;
using System.Linq;

namespace Rebus.Bus
{
    class DueTimeoutScheduler : IDisposable
    {
        static ILog log;

        static DueTimeoutScheduler()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly IStoreTimeouts storeTimeouts;
        readonly IHandleDeferredMessage handleDeferredMessage;
        readonly Timer timer = new Timer();
        volatile bool currentlyChecking;
        readonly object checkLock = new object();

        public DueTimeoutScheduler(IStoreTimeouts storeTimeouts, IHandleDeferredMessage handleDeferredMessage)
        {
            this.storeTimeouts = storeTimeouts;
            this.handleDeferredMessage = handleDeferredMessage;
            timer.Interval = TimeSpan.FromSeconds(0.3).TotalMilliseconds;
            timer.Elapsed += CheckCallbacks;
         
            log.Info("Starting due timeouts scheduler");
            timer.Start();
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

                    var dueTimeouts = storeTimeouts.GetDueTimeouts().ToList();
                    if (!dueTimeouts.Any()) return;
                    
                    log.Info("Got {0} dues timeouts - will send them now", dueTimeouts.Count);
                    foreach (var timeout in dueTimeouts)
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

                        handleDeferredMessage.SendReply(timeout.ReplyTo, reply, sagaId);

                        timeout.MarkAsProcessed();
                    }
                }
                finally
                {
                    currentlyChecking = false;
                }
            }
        }

        public void Dispose()
        {
            log.Info("Stopping due timeouts scheduler");
            timer.Stop();
            timer.Dispose();
        }
    }
}
