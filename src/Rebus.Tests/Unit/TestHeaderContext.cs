using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Shouldly;
using System.Linq;

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

        /// <summary>
        /// Starting point:
        ///     1000 + 1000 took 10,6 s
        /// 
        /// Pre-lookup by object hash code:
        ///     1000 + 1000 took 0,4 s
        /// </summary>
        [TestCase(1000,1000)]
        [TestCase(100,1000)]
        [TestCase(1000,100)]
        [TestCase(100,100)]
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
            c.headers[firstObject.GetHashCode()].Single(s => s.Target == firstObject).Headers.ShouldContainKeyAndValue("first-header1", "first-value");
            c.headers[firstObject.GetHashCode()].Single(s => s.Target == firstObject).Headers.ShouldContainKeyAndValue("first-header2", "first-value");
            c.headers[secondObject.GetHashCode()].Single(s => s.Target == secondObject).Headers.ShouldContainKeyAndValue("second-header", "first-value");
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