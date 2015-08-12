using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Shouldly;
using System.Linq;
using Timer = System.Timers.Timer;

namespace Rebus.Tests.Unit
{
    [TestFixture]
    public class TestHeaderContext : FixtureBase
    {
        RebusBus.HeaderContext c;

        protected override void DoSetUp()
        {
            c = new RebusBus.HeaderContext();
        }

        [TestCase(20, 20000)]
        [TestCase(30, 50000)]
        [TestCase(40, 50000, Ignore = true)]
        [TestCase(40, 200000, Ignore = true)]
        public void IsReliableAlsoWhenUsedByManyThreads(int howManyThreads, int numberOfIterations)
        {
            var reads = 0;
            var writes = 0;
            var headersAttached = 0;
            var headersActuallyFound = 0;

            Action printStats = () => Console.WriteLine("Reads: {0}. Writes: {1}. Buckets: {2}.", reads, writes, c.headers.Count);

            using (var printTimer = new Timer())
            {
                printTimer.Interval = 2000;
                printTimer.Elapsed += (s, a) => printStats();
                printTimer.Start();

                var threads = Enumerable
                    .Range(1, howManyThreads)
                    .Select(no => string.Format("Thread#{0}", no))
                    .Select(name => new Thread(() =>
                    {
                        var random = new Random();
                        var messages = new List<object>();

                        for (var count = 0; count < numberOfIterations; count++)
                        {
                            if (random.Next(2) == 0)
                            {
                                var message = new object();
                                messages.Add(message);
                                c.AttachHeader(message, name + ".key", "some value");
                                Interlocked.Increment(ref headersAttached);
                                Interlocked.Increment(ref writes);
                            }
                            else
                            {
                                var randomMessage = messages[random.Next(messages.Count)];
                                var headers = c.GetHeadersFor(randomMessage);

                                string randomKey;
                                do
                                {
                                    randomKey = name + ".random." + random.Next(10000);
                                } while (headers.ContainsKey(randomKey));

                                Interlocked.Increment(ref headersAttached);
                                c.AttachHeader(randomMessage, randomKey, "RANDOOOOM!");
                                Interlocked.Increment(ref reads);
                            }
                        }

                        foreach (var message in messages)
                        {
                            Interlocked.Add(ref headersActuallyFound, c.GetHeadersFor(message).Count);
                        }
                    }))
                    .ToList();

                threads.ForEach(t => t.Start());
                threads.ForEach(t => t.Join());
            }

            printStats();
            Console.WriteLine("Expected total of {0} headers - found {1}", headersAttached, headersActuallyFound);
        }

        /// <summary>
        /// Starting point:
        ///     1000 + 1000 took 10,6 s
        /// 
        /// Pre-lookup by object hash code:
        ///     1000 + 1000 took 0,4 s
        /// 
        /// Max # buckets = 256 (modulo on hash code)
        ///     1000 + 1000 took 0,4 s
        /// 
        /// Max # buckets = 512
        ///     1000 + 1000 took 0,3 s
        /// 
        /// Max # buckets = 1024
        ///     1000 + 1000 took 0,5 s
        ///
        /// Max # buckets = 8192 
        ///     1000 + 1000 took 1,2 s
        /// </summary>
        [TestCase(1000, 1000)]
        [TestCase(100, 1000)]
        [TestCase(1000, 100)]
        [TestCase(100, 100)]
        public void TestPerformance(int addIterations, int getIterations)
        {
            var stopwatch = Stopwatch.StartNew();
            var messages = new List<object>();

            addIterations.Times(() =>
            {
                var message = new object();
                messages.Add(message);

                c.AttachHeader(message, "whatever", "some value");
            });

            getIterations.Times(() =>
            {
                foreach (var message in messages)
                {
                    var customHeaders = c.GetHeadersFor(message);

                    customHeaders.ShouldContainKeyAndValue("whatever", "some value");
                }
            });

            Console.WriteLine("{0} + {1} took {2:0.0} s", addIterations, getIterations, stopwatch.Elapsed.TotalSeconds);
        }

        [Test]
        public void AssociatesHeadersWithObjects()
        {
            // arrange
            var firstObject = new object();
            var secondObject = new object();

            // act
            c.AttachHeader(firstObject, "first-header1", "first-value");
            c.AttachHeader(firstObject, "first-header2", "first-value");
            c.AttachHeader(secondObject, "second-header", "first-value");

            // assert
            c.headers[RebusBus.HeaderContext.GetHashCodeFromMessage(firstObject)].Single(s => s.Target == firstObject).Headers.ShouldContainKeyAndValue("first-header1", "first-value");
            c.headers[RebusBus.HeaderContext.GetHashCodeFromMessage(firstObject)].Single(s => s.Target == firstObject).Headers.ShouldContainKeyAndValue("first-header2", "first-value");
            c.headers[RebusBus.HeaderContext.GetHashCodeFromMessage(secondObject)].Single(s => s.Target == secondObject).Headers.ShouldContainKeyAndValue("second-header", "first-value");
        }

        [Test]
        public void UsesWeakReferencesToKeepAssociation()
        {
            // arrange
            var someObject = new object();
            c.AttachHeader(someObject, "header1", "value1");
            c.AttachHeader(someObject, "header2", "value2");

            // just check that the dictionary is there
            c.headers.First().Value.Count.ShouldBe(1);

            // act
            someObject = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            c.Tick();

            // assert
            c.headers.First().Value.Count.ShouldBe(0);
        }

        [Test]
        public void CleansUpPeriodically()
        {
            // arrange
            var someObject = new object();
            c.AttachHeader(someObject, "header1", "value1");

            c.headers.First().Value.Count.ShouldBe(1);

            // act
            someObject = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Thread.Sleep(2000);

            // assert
            c.headers.First().Value.Count.ShouldBe(0);
        }
    }

    public static class AssertionExtensions
    {
        public static void ShouldContainKeyAndValue(this Tuple<WeakReference, Dictionary<string, object>> tuple, string key, string value)
        {
            var dictionary = tuple.Item2;

            dictionary.ShouldContainKeyAndValue(key, value);
        }
    }
}