using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Time;
using Rebus.Timeouts;

namespace Rebus.Tests.Contracts.Timeouts
{
    public class BasicStoreAndRetrieveOperations<TTimeoutManagerFactory> : FixtureBase where TTimeoutManagerFactory : ITimeoutManagerFactory, new()
    {
        TTimeoutManagerFactory _factory;
        ITimeoutManager _timeoutManager;

        protected override void SetUp()
        {
            _factory = new TTimeoutManagerFactory();
            _timeoutManager = _factory.Create();
        }

        protected override void TearDown()
        {
            _factory.Cleanup();
        }

        [Test]
        public async Task DoesNotLoadAnythingInitially()
        {
            using (var result = await _timeoutManager.GetDueMessages())
            {
                Assert.That(result.Count(), Is.EqualTo(0));
            }
        }

        [Test]
        public async Task ImmediatelyGetsTimeoutWhenItIsDueInThePast()
        {
            await _timeoutManager.Defer(DateTimeOffset.Now.AddMinutes(-1), HeadersWith("i know u"), EmptyBody());

            using (var result = await _timeoutManager.GetDueMessages())
            {
                var dueTimeouts = result.ToList();

                Assert.That(dueTimeouts.Count, Is.EqualTo(1));
                Assert.That(dueTimeouts[0].Headers[Headers.MessageId], Is.EqualTo("i know u"));
            }
        }

        [Test]
        public async Task TimeoutsAreNotReturnedUntilTheyAreActuallyDue()
        {
            var theFuture = DateTimeOffset.Now.AddMinutes(1);
            var evenFurtherIntoTheFuture = DateTimeOffset.Now.AddMinutes(8);

            await _timeoutManager.Defer(theFuture, HeadersWith("i know u"), EmptyBody());
            await _timeoutManager.Defer(evenFurtherIntoTheFuture, HeadersWith("i know u too"), EmptyBody());

            using (var result = await _timeoutManager.GetDueMessages())
            {
                var dueTimeoutsNow = result.ToList();

                Assert.That(dueTimeoutsNow.Count, Is.EqualTo(0));
            }

            RebusTimeMachine.FakeIt(theFuture);

            using (var result = await _timeoutManager.GetDueMessages())
            {
                var dueTimeoutsInTheFuture = result.ToList();
                Assert.That(dueTimeoutsInTheFuture.Count, Is.EqualTo(1));
                Assert.That(dueTimeoutsInTheFuture[0].Headers[Headers.MessageId], Is.EqualTo("i know u"));
            }

            RebusTimeMachine.FakeIt(evenFurtherIntoTheFuture);

            using (var result = await _timeoutManager.GetDueMessages())
            {
                var dueTimeoutsFurtherIntoInTheFuture = result.ToList();
                Assert.That(dueTimeoutsFurtherIntoInTheFuture.Count, Is.EqualTo(1));
                Assert.That(dueTimeoutsFurtherIntoInTheFuture[0].Headers[Headers.MessageId], Is.EqualTo("i know u too"));
            }
        }

        static Dictionary<string, string> HeadersWith(string id)
        {
            return new Dictionary<string, string>
            {
                { Headers.MessageId, id }
            };
        }

        static byte[] EmptyBody()
        {
            return new byte[0];
        }
    }
}