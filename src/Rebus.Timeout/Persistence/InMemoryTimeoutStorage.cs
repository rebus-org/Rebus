using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Timeout.Persistence
{
    public class InMemoryTimeoutStorage : IStoreTimeouts
    {
        readonly object listLock = new object();
        readonly List<Timeout> timeouts = new List<Timeout>();

        public void Add(Timeout newTimeout)
        {
            lock (listLock)
            {
                timeouts.Add(newTimeout);
            }
        }

        public IEnumerable<Timeout> RemoveDueTimeouts()
        {
            lock (listLock)
            {
                var timeoutsToRemove = timeouts.Where(t => t.TimeToReturn >= DateTime.UtcNow).ToList();

                timeoutsToRemove.ForEach(t => timeouts.Remove(t));

                return timeoutsToRemove;
            }
        }
    }
}