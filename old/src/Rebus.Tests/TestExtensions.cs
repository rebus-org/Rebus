using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace Rebus.Tests
{
    public static class TestExtensions
    {
        public static IEnumerable<List<T>> Partition<T>(this IEnumerable<T> items, int partitionSize)
        {
            List<T> batch;
            var skip = 0;
            var allItems = items.ToList();

            do
            {
                batch = allItems.Skip(skip)
                                .Take(partitionSize)
                                .ToList();

                if (batch.Any())
                {
                    yield return batch;
                }

                skip += partitionSize;
            } while (batch.Any());
        }
        
        public static void Times(this int count, Action action)
        {
            for (var counter = 0; counter < count; counter++)
            {
                action();
            }
        }

        public static void WaitUntilSetOrDie(this ManualResetEvent resetEvent, TimeSpan timeout, string errorMessage, params object[] objs)
        {
            if (resetEvent.WaitOne(timeout)) return;

            var message = string.Format("Event was not set within {0} timeout: ", timeout)
                         + string.Format(errorMessage, objs);

            throw new AssertionException(message);
        }
        
        public static void WaitUntilSetOrDie(this AutoResetEvent resetEvent, TimeSpan timeout, string errorMessage, params object[] objs)
        {
            if (resetEvent.WaitOne(timeout)) return;

            var message = string.Format("Event was not set within {0} timeout: ", timeout)
                         + string.Format(errorMessage, objs);

            throw new AssertionException(message);
        }
        
        public static void WaitUntilSetOrDie(this ManualResetEvent resetEvent, TimeSpan timeout)
        {
            if (resetEvent.WaitOne(timeout)) return;

            var message = string.Format("Event was not set within {0} timeout", timeout);

            throw new AssertionException(message);
        }

        public static void WaitUntilSetOrDie(this AutoResetEvent resetEvent, TimeSpan timeout)
        {
            if (resetEvent.WaitOne(timeout)) return;

            var message = string.Format("Event was not set within {0} timeout", timeout);

            throw new AssertionException(message);
        }

        [DebuggerStepThrough]
        public static int CountOcurrencesOf(this string input, string pattern)
        {
            if (input == null) throw new ArgumentNullException("input", "Cannot count occurrences in a null string");
            if (pattern == null) throw new ArgumentNullException("pattern", string.Format("Cannot count occurrences of (null) in the string {0}", input));

            return input.Split(new[] { pattern }, StringSplitOptions.None).Length - 1;
        }

        [DebuggerStepThrough]
        public static TimeSpan Milliseconds(this int seconds)
        {
            return TimeSpan.FromMilliseconds(seconds);
        }

        [DebuggerStepThrough]
        public static TimeSpan Seconds(this int seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        [DebuggerStepThrough]
        public static TimeSpan Seconds(this double seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        [DebuggerStepThrough]
        public static TimeSpan ElapsedSince(this DateTime someTime, DateTime somePastTime)
        {
            return someTime - somePastTime;
        }

        public static ReceivedTransportMessage ToReceivedTransportMessage(this TransportMessageToSend message)
        {
            return new ReceivedTransportMessage
                       {
                           Headers = message.Headers,
                           Body = message.Body,
                           Label = message.Label,
                           Id = Guid.NewGuid()
                                    .ToString()
                       };
        }
    }
}