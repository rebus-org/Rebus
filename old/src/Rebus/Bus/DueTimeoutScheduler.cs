using System;
using System.Collections.Generic;
using System.Timers;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Timeout;
using System.Linq;

namespace Rebus.Bus
{
    /// <summary>
    /// Responsible for periodically checking the timeout storage for due timeouts in the given implementation of <see cref="IStoreTimeouts"/>.
    /// Due timeouts will result in a <see cref="TimeoutReply"/> which will be dispatched using the given implementation of <see cref="IHandleDeferredMessage"/>.
    /// </summary>
    class DueTimeoutScheduler : IDisposable
    {
        static ILog log;

        static DueTimeoutScheduler()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        static readonly TimeSpan MaxAcceptedFailureTimeBeforeLogLevelEscalation = TimeSpan.FromMinutes(1);

        readonly IStoreTimeouts storeTimeouts;
        readonly IHandleDeferredMessage handleDeferredMessage;
        readonly Timer timer = new Timer();

        readonly object checkLock = new object();
        volatile bool currentlyChecking;

        DateTime lastSuccess;

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

                    using (var dueTimeoutsResult = GetDueTimeouts())
                    {
                        var dueTimeouts = dueTimeoutsResult.DueTimeouts.ToList();
                        if (!dueTimeouts.Any()) return;

                        log.Info("Got {0} dues timeouts - will send them now", dueTimeouts.Count);
                        foreach (var timeout in dueTimeouts)
                        {
                            log.Info("Timeout!: {0} -> {1}", timeout.CorrelationId, timeout.ReplyTo);

                            var reply = new TimeoutReply
                            {
                                SagaId = timeout.SagaId,
                                CorrelationId = timeout.CorrelationId,
                                DueTime = timeout.TimeToReturn,
                                CustomData = timeout.CustomData,
                            };

                            SendReply(timeout, reply);

                            MarkAsProcessed(timeout);
                        }
                    }

                    lastSuccess = DateTime.UtcNow;
                }
                catch (Exception exception)
                {
                    var timeSinceLastSuccess = DateTime.UtcNow-lastSuccess;

                    if (timeSinceLastSuccess > MaxAcceptedFailureTimeBeforeLogLevelEscalation)
                    {
                        log.Error(exception, "An error occurred while attempting to retrieve due timeouts and send timeout replies! The situation has been going on for {0}", timeSinceLastSuccess);
                    }
                    else
                    {
                        log.Warn(
                            "An error occurred while attempting to retrieve due timeouts and send timeout replies: {0} - if the situation persists for more than {1}, the log level will escalate from WARN to ERROR",
                            exception, MaxAcceptedFailureTimeBeforeLogLevelEscalation);
                    }
                }
                finally
                {
                    currentlyChecking = false;
                }
            }
        }

        void MarkAsProcessed(DueTimeout timeout)
        {
            try
            {
                timeout.MarkAsProcessed();
            }
            catch (Exception exception)
            {
                throw new ApplicationException(string.Format("Could not mark timeout {0} as processed!", timeout), exception);
            }
        }

        void SendReply(DueTimeout timeout, TimeoutReply reply)
        {
            try
            {
                handleDeferredMessage.SendReply(timeout.ReplyTo, reply, timeout.SagaId);
            }
            catch (Exception exception)
            {
                throw new ApplicationException(string.Format("An error occurred while attempting to send reply for {0}", timeout), exception);
            }
        }

        DueTimeoutsResult GetDueTimeouts()
        {
            try
            {
                return storeTimeouts.GetDueTimeouts();
            }
            catch (Exception exception)
            {
                throw new ApplicationException("Could not retrieve due timeouts!", exception);
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
