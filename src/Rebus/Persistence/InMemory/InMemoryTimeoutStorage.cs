using System;
using System.Collections.Generic;
using System.Linq;
using Rebus.Timeout;

namespace Rebus.Persistence.InMemory
{
    public class InMemoryTimeoutStorage : IStoreTimeouts
    {
        readonly object listLock = new object();
        readonly List<Timeout.Timeout> timeouts = new List<Timeout.Timeout>();

        public void Add(Timeout.Timeout newTimeout)
        {
            lock (listLock)
            {
                timeouts.Add(newTimeout);
            }
        }

        public IEnumerable<Timeout.Timeout> RemoveDueTimeouts()
        {
            lock (listLock)
            {
                var timeoutsToRemove = timeouts.Where(t => RebusTimeMachine.Now() >= t.TimeToReturn).ToList();

                timeoutsToRemove.ForEach(t => timeouts.Remove(t));

                return timeoutsToRemove;
            }
        }
    }
}