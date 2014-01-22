using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rebus.Bus;

namespace Rebus.Configuration
{
    /// <summary>
    /// Defines the worker thread back off behavior.
    /// </summary>
    public class BackoffBehavior : IEnumerable<TimeSpan>
    {
        public static BackoffBehavior Default()
        {
            return new BackoffBehavior
            {
                // first 2 s
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(200),

                // next 10 s
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),

                // the rest of the time
                TimeSpan.FromMilliseconds(5000)
            };
        }

        public static BackoffBehavior LowLatency()
        {
            return new BackoffBehavior { TimeSpan.FromMilliseconds(20) };
        }

        readonly List<TimeSpan> backoffTimes = new List<TimeSpan>();

        public void Add(TimeSpan backoffTime)
        {
            backoffTimes.Add(backoffTime);
        }

        public IEnumerator<TimeSpan> GetEnumerator()
        {
            return backoffTimes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}