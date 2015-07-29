using System;
using System.Collections.Generic;
using System.Linq;
using Rebus.Logging;
using Rebus.Timeout;

namespace Rebus.Persistence.InMemory
{
    /// <summary>
    /// Timeout storage that stores timeouts in memory. Please don't use this for anything other than testing stuff,
    /// e.g. on your developer box and possibly in test environments.
    /// </summary>
    public class InMemoryTimeoutStorage : IStoreTimeouts
    {
        static ILog log;

        static InMemoryTimeoutStorage()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly object listLock = new object();
        readonly List<Timeout.Timeout> timeouts = new List<Timeout.Timeout>();

        /// <summary>
        /// Stores the given timeout in memory
        /// </summary>
        public void Add(Timeout.Timeout newTimeout)
        {
            log.Debug("Adding timeout {0} -> {1}: {2}",
                      newTimeout.TimeToReturn,
                      newTimeout.ReplyTo,
                      newTimeout.CustomData);

            lock (listLock)
            {
                timeouts.Add(newTimeout);
            }
        }

        /// <summary>
        /// Retrieves all due timeouts
        /// </summary>
        public DueTimeoutsResult GetDueTimeouts()
        {
            lock (listLock)
            {
                var timeoutsToRemove = timeouts
                    .Where(t => RebusTimeMachine.Now() >= t.TimeToReturn)
                    .ToList();

                log.Debug("Returning {0} timeouts", timeoutsToRemove.Count);

                return new DueTimeoutsResult(timeoutsToRemove
                    .Select(t => new DueInMemoryTimeout(t.ReplyTo,
                        t.CorrelationId,
                        t.TimeToReturn,
                        t.SagaId,
                        t.CustomData,
                        () =>
                        {
                            lock (listLock)
                            {
                                log.Debug("Removing timeout {0} -> {1}: {2}", t.TimeToReturn, t.ReplyTo, t.CustomData);

                                timeouts.Remove(t);
                            }
                        }))
                    .ToList());
            }
        }

        class DueInMemoryTimeout : DueTimeout
        {
            readonly Action removeAction;

            public DueInMemoryTimeout(string replyTo, string correlationId, DateTime timeToReturn, Guid sagaId, string customData, Action removeAction) 
                : base(replyTo, correlationId, timeToReturn, sagaId, customData)
            {
                this.removeAction = removeAction;
            }

            public override void MarkAsProcessed()
            {
                removeAction();
            }
        }
    }
}