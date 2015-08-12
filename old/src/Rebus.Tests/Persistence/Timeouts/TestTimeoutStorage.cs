using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rebus.Tests.Persistence.Timeouts.Factories;
using Rebus.Timeout;
using Shouldly;

namespace Rebus.Tests.Persistence.Timeouts
{
    [TestFixture(typeof(InMemoryTimeoutStorageFactory))]
    [TestFixture(typeof(SqlServerTimeoutStorageFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(PostgreSqlServerTimeoutStorageFactory), Category = TestCategories.PostgreSql)]
    [TestFixture(typeof(RavenDbTimeoutStorageFactory), Category = TestCategories.Raven)]
    [TestFixture(typeof(MongoDbTimeoutStorageFactory), Category = TestCategories.Mongo)]
    public class TestTimeoutStorage<TFactory> : FixtureBase where TFactory : ITimeoutStorageFactory
    {
        TFactory factory;
        IStoreTimeouts storage;

        protected override void DoSetUp()
        {
            factory = Activator.CreateInstance<TFactory>();
            storage = factory.CreateStore();
        }

        protected override void DoTearDown()
        {
            factory.Dispose();
        }

        [Test]
        public void DoesNotActuallyRemoveTimeoutUntilItIsMarkedAsProcessed()
        {
            // arrange
            const string someRecognizablePieceOfCustomData = "this custom dizzle can be recognizzle!!";

            var actualTimeWhenIWroteThis = new DateTime(2012, 11, 30, 22, 13, 00, DateTimeKind.Utc);
            storage.Add(new Rebus.Timeout.Timeout("someone", "wham!", actualTimeWhenIWroteThis.AddSeconds(20), Guid.Empty, someRecognizablePieceOfCustomData));
            TimeMachine.FixTo(actualTimeWhenIWroteThis.AddSeconds(25));

            // act
            List<DueTimeout> dueTimeoutsFromFirstCall;
            using (var dueTimeoutsResult = storage.GetDueTimeouts())
            {
                dueTimeoutsFromFirstCall = dueTimeoutsResult.DueTimeouts.ToList();
            }

            // this is where we'd have marked the due timeout as processed - instead, we pretend that didn't happen
            // (perhaps because the timeout service was interrupted) ...
            List<DueTimeout> dueTimeoutsFromSecondCall;
            using (var nextDueTimeoutsResult = storage.GetDueTimeouts())
            {
                dueTimeoutsFromSecondCall = nextDueTimeoutsResult.DueTimeouts.ToList();
            }

            // assert
            dueTimeoutsFromFirstCall.Count.ShouldBe(1);
            dueTimeoutsFromFirstCall.Single().CustomData.ShouldBe(someRecognizablePieceOfCustomData);
            
            dueTimeoutsFromSecondCall.Count().ShouldBe(1);
            dueTimeoutsFromSecondCall.Single().CustomData.ShouldBe(someRecognizablePieceOfCustomData);
        }

        [Test]
        public void DoesNotComplainWhenTheSameTimeoutIsAddedMultipleTimes()
        {
            var justSomeTime = new DateTime(2010, 1, 1, 10, 30, 0, DateTimeKind.Utc);

            storage.Add(new Rebus.Timeout.Timeout("blah blah", "blah", justSomeTime, Guid.Empty, null));
            storage.Add(new Rebus.Timeout.Timeout("blah blah", "blah", justSomeTime, Guid.Empty, null));
            storage.Add(new Rebus.Timeout.Timeout("blah blah", "blah", justSomeTime, Guid.Empty, null));
        }

        [Test]
        public void CanStoreAndRemoveTimeouts()
        {
            var someUtcTimeStamp = new DateTime(2010, 3, 10, 12, 30, 15, DateTimeKind.Utc);
            var anotherUtcTimeStamp = someUtcTimeStamp.AddHours(2);
            var thirtytwoKilobytesOfDollarSigns = new string('$', 32768);

            storage.Add(new Rebus.Timeout.Timeout("somebody",
                                                  "first", someUtcTimeStamp,
                                                  new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                                                  null));

            storage.Add(new Rebus.Timeout.Timeout("somebody",
                                                  "second",
                                                  anotherUtcTimeStamp,
                                                  new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                                                  thirtytwoKilobytesOfDollarSigns));

            TimeMachine.FixTo(someUtcTimeStamp.AddSeconds(-1));

            var dueTimeoutsBeforeTimeout = GetTimeouts();
            dueTimeoutsBeforeTimeout.Count().ShouldBe(0);

            TimeMachine.FixTo(someUtcTimeStamp.AddSeconds(1));

            using (var firstTimeoutsResult = storage.GetDueTimeouts())
            {
                var dueTimeoutsAfterFirstTimeout = firstTimeoutsResult.DueTimeouts;
                var firstTimeout = dueTimeoutsAfterFirstTimeout.SingleOrDefault();
                firstTimeout.ShouldNotBe(null);
                firstTimeout.SagaId.ShouldBe(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
                firstTimeout.CorrelationId.ShouldBe("first");
                firstTimeout.ReplyTo.ShouldBe("somebody");
                firstTimeout.TimeToReturn.ShouldBe(someUtcTimeStamp);
                firstTimeout.MarkAsProcessed();
            }

            var dueTimeoutsAfterHavingMarkingTheFirstTimeoutAsProcessed = GetTimeouts();
            dueTimeoutsAfterHavingMarkingTheFirstTimeoutAsProcessed.Count().ShouldBe(0);

            TimeMachine.FixTo(anotherUtcTimeStamp.AddSeconds(1));

            using (var secondTimeoutsResult = storage.GetDueTimeouts())
            {
                var dueTimeoutsAfterSecondTimeout = secondTimeoutsResult.DueTimeouts;
                var secondTimeout = dueTimeoutsAfterSecondTimeout.SingleOrDefault();
                secondTimeout.ShouldNotBe(null);
                secondTimeout.SagaId.ShouldBe(new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
                secondTimeout.CorrelationId.ShouldBe("second");
                secondTimeout.ReplyTo.ShouldBe("somebody");
                secondTimeout.TimeToReturn.ShouldBe(anotherUtcTimeStamp);
                secondTimeout.CustomData.ShouldBe(thirtytwoKilobytesOfDollarSigns);
                secondTimeout.MarkAsProcessed();
            }

            GetTimeouts().Count().ShouldBe(0);
        }

        IEnumerable<DueTimeout> GetTimeouts()
        {
            using (var dueTimeoutsResult = storage.GetDueTimeouts())
            {
                return dueTimeoutsResult.DueTimeouts.ToList();
            }
        }

        [Test]
        public void CanRemoveMultipleTimeoutsAtOnce()
        {
            var justSomeUtcTimeStamp = new DateTime(2010, 3, 10, 12, 30, 15, DateTimeKind.Utc);

            storage.Add(new Rebus.Timeout.Timeout("somebody", "first", justSomeUtcTimeStamp, Guid.Empty, null));
            storage.Add(new Rebus.Timeout.Timeout("somebody", "second", justSomeUtcTimeStamp, Guid.Empty, null));

            TimeMachine.FixTo(justSomeUtcTimeStamp.AddSeconds(1));

            using (var dueTimeoutsResult = storage.GetDueTimeouts())
            {
                var dueTimeoutsAfterFirstTimeout = dueTimeoutsResult.DueTimeouts;

                dueTimeoutsAfterFirstTimeout.Count().ShouldBe(2);
            }
        }
    }
}