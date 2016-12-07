using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Time;
using Rebus.Timeouts;
using Xunit;

namespace Rebus.Tests.Contracts.Timeouts
{
    /// <summary>
    /// Test fixture base class for verifying compliance with the <see cref="ITimeoutManager"/> contract
    /// </summary>
    public abstract class BasicStoreAndRetrieveOperations<TTimeoutManagerFactory> : FixtureBase where TTimeoutManagerFactory : ITimeoutManagerFactory, new()
    {
        TTimeoutManagerFactory _factory;
        ITimeoutManager _timeoutManager;

        protected BasicStoreAndRetrieveOperations()
        {
            _factory = new TTimeoutManagerFactory();
            _timeoutManager = _factory.Create();
        }

        protected override void TearDown()
        {
            _factory.Cleanup();
        }

        [Fact]
        public async Task DoesNotLoadAnythingInitially()
        {
            using (var result = await _timeoutManager.GetDueMessages())
            {
                Assert.Equal(0, result.Count());
            }
        }

        [Fact]
        public async Task IsCapableOfReturningTheMessageBodyAsWell()
        {
            var someTimeInThePast = RebusTime.Now.AddMinutes(-1);

            const string stringBody = "hello there!";

            await _timeoutManager.Defer(someTimeInThePast, HeadersWith("i know u"), Encoding.UTF8.GetBytes(stringBody));

            using (var result = await _timeoutManager.GetDueMessages())
            {
                var dueTimeouts = result.ToList();

                Assert.Equal(1, dueTimeouts.Count);

                var bodyBytes = dueTimeouts[0].Body;

                Assert.Equal(stringBody, Encoding.UTF8.GetString(bodyBytes));
            }
        }

        [Fact]
        public async Task ImmediatelyGetsTimeoutWhenItIsDueInThePast()
        {
            var someTimeInThePast = RebusTime.Now.AddMinutes(-1);

            await _timeoutManager.Defer(someTimeInThePast, HeadersWith("i know u"), EmptyBody());

            using (var result = await _timeoutManager.GetDueMessages())
            {
                var dueTimeouts = result.ToList();

                Assert.Equal(1, dueTimeouts.Count);
                Assert.Equal("i know u", dueTimeouts[0].Headers[Headers.MessageId]);
            }
        }

        [Fact]
        public async Task TimeoutsAreNotRemovedIfTheyAreNotMarkedAsComplete()
        {
            var theFuture = RebusTime.Now.AddMinutes(1);

            await _timeoutManager.Defer(theFuture, HeadersWith("i know u"), EmptyBody());
            
            RebusTimeMachine.FakeIt(theFuture + TimeSpan.FromSeconds(1));

            using (var result = await _timeoutManager.GetDueMessages())
            {
                var dueTimeoutsInTheFuture = result.ToList();
                Assert.Equal(1, dueTimeoutsInTheFuture.Count);
            }

            using (var result = await _timeoutManager.GetDueMessages())
            {
                var dueTimeoutsInTheFuture = result.ToList();
                Assert.Equal(1, dueTimeoutsInTheFuture.Count);

                // mark as complete
                await dueTimeoutsInTheFuture[0].MarkAsCompleted();
            }
            
            using (var result = await _timeoutManager.GetDueMessages())
            {
                var dueTimeoutsInTheFuture = result.ToList();
                Assert.Equal(0, dueTimeoutsInTheFuture.Count);
            }
        }

        [Fact]
        public async Task TimeoutsAreNotReturnedUntilTheyAreActuallyDue()
        {
            var theFuture = DateTimeOffset.Now.AddMinutes(1);
            var evenFurtherIntoTheFuture = DateTimeOffset.Now.AddMinutes(8);

            await _timeoutManager.Defer(theFuture, HeadersWith("i know u"), EmptyBody());
            await _timeoutManager.Defer(evenFurtherIntoTheFuture, HeadersWith("i know u too"), EmptyBody());

            using (var result = await _timeoutManager.GetDueMessages())
            {
                var dueTimeoutsNow = result.ToList();

                Assert.Equal(0, dueTimeoutsNow.Count);
            }

            RebusTimeMachine.FakeIt(theFuture);

            using (var result = await _timeoutManager.GetDueMessages())
            {
                var dueTimeoutsInTheFuture = result.ToList();
                Assert.Equal(1, dueTimeoutsInTheFuture.Count);
                Assert.Equal("i know u", dueTimeoutsInTheFuture[0].Headers[Headers.MessageId]);

                await dueTimeoutsInTheFuture[0].MarkAsCompleted();
            }

            RebusTimeMachine.FakeIt(evenFurtherIntoTheFuture);

            using (var result = await _timeoutManager.GetDueMessages())
            {
                var dueTimeoutsFurtherIntoInTheFuture = result.ToList();
                Assert.Equal(1, dueTimeoutsFurtherIntoInTheFuture.Count);
                Assert.Equal("i know u too", dueTimeoutsFurtherIntoInTheFuture[0].Headers[Headers.MessageId]);

                await dueTimeoutsFurtherIntoInTheFuture[0].MarkAsCompleted();
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