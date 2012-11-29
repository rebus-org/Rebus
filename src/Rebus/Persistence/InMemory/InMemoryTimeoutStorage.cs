using System;
using System.Collections.Generic;
using System.Linq;
using Rebus.Timeout;

namespace Rebus.Persistence.InMemory
{
    /// <summary>
    /// Timeout storage that stores timeouts in memory. Please don't use this for anything other than testing stuff,
    /// e.g. on your developer box and possibly in test environments.
    /// </summary>
    public class InMemoryTimeoutStorage : IStoreTimeouts
    {
        readonly object listLock = new object();
        readonly List<Timeout.Timeout> timeouts = new List<Timeout.Timeout>();

        /// <summary>
        /// Stores the given timeout in memory
        /// </summary>
        public void Add(Timeout.Timeout newTimeout)
        {
            lock (listLock)
            {
                timeouts.Add(newTimeout);
            }
        }

        /// <summary>
        /// Destructively retrieves all due timeouts
        /// </summary>
        public IEnumerable<DueTimeout> GetDueTimeouts()
        {
            lock (listLock)
            {
                var timeoutsToRemove = timeouts.Where(t => RebusTimeMachine.Now() >= t.TimeToReturn).ToList();

                return timeoutsToRemove
                    .Select(t => new DueInMemoryTimeout(t.ReplyTo,
                                                        t.CorrelationId,
                                                        t.TimeToReturn,
                                                        t.SagaId,
                                                        t.CustomData,
                                                        () => timeouts.Remove(t)));
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