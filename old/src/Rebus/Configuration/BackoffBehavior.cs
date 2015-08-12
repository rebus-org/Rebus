using System;
using System.Collections;
using System.Collections.Generic;

namespace Rebus.Configuration
{
    /// <summary>
    /// Defines the worker thread back off behavior.
    /// </summary>
    public class BackoffBehavior : IEnumerable<TimeSpan>
    {
        /// <summary>
        /// Returns the default backoff behavior, which is a compromise between low latency and not thrashing the queueing system too hard.
        /// </summary>
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

        /// <summary>
        /// Returns a backoff behavior, that will only wait a short while between re-polling the queueing system, which may lead to
        /// putting slightly more load on the queueing system.
        /// </summary>
        public static BackoffBehavior LowLatency()
        {
            return new BackoffBehavior { TimeSpan.FromMilliseconds(20) };
        }

        readonly List<TimeSpan> backoffTimes = new List<TimeSpan>();

        /// <summary>
        /// Adds the given backoff interval to the collection of backoff times
        /// </summary>
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