using System;
using System.Linq;
using NUnit.Framework;
using Rebus.Tests.Persistence.Timeouts.Factories;
using Rebus.Timeout;
using Shouldly;

namespace Rebus.Tests.Persistence.Timeouts
{
    [TestFixture(typeof(InMemoryTimeoutStorageFactory))]
    [TestFixture(typeof(SqlServerTimeoutStorageFactory), Category = TestCategories.MsSql)]
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
        public void DoesNotComplainWhenTheSameTimeoutIsAddedMultipleTimes()
        {
            var justSomeTime = new DateTime(2010, 1, 1, 10, 30, 0, DateTimeKind.Utc);

            storage.Add(new Rebus.Timeout.Timeout { CorrelationId = "blah", ReplyTo = "blah blah", TimeToReturn = justSomeTime });
            storage.Add(new Rebus.Timeout.Timeout { CorrelationId = "blah", ReplyTo = "blah blah", TimeToReturn = justSomeTime });
            storage.Add(new Rebus.Timeout.Timeout { CorrelationId = "blah", ReplyTo = "blah blah", TimeToReturn = justSomeTime });
        }

        [Test]
        public void CanStoreAndRemoveTimeouts()
        {
            var someUtcTimeStamp = new DateTime(2010, 3, 10, 12, 30, 15, DateTimeKind.Utc);
            var anotherUtcTimeStamp = someUtcTimeStamp.AddHours(2);
            var thirtytwoKilobytesOfDollarSigns = new string('$', 32768);

            storage.Add(new Rebus.Timeout.Timeout
                            {
                                SagaId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                                CorrelationId = "first",
                                ReplyTo = "somebody",
                                TimeToReturn = someUtcTimeStamp,
                                CustomData = null,
                            });

            storage.Add(new Rebus.Timeout.Timeout
                            {
                                SagaId = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                                CorrelationId = "second",
                                ReplyTo = "somebody",
                                TimeToReturn = anotherUtcTimeStamp,
                                CustomData = thirtytwoKilobytesOfDollarSigns,
                            });

            TimeMachine.FixTo(someUtcTimeStamp.AddSeconds(-1));

            var dueTimeoutsBeforeTimeout = storage.RemoveDueTimeouts();
            dueTimeoutsBeforeTimeout.Count().ShouldBe(0);

            TimeMachine.FixTo(someUtcTimeStamp.AddSeconds(1));

            var dueTimeoutsAfterFirstTimeout = storage.RemoveDueTimeouts();
            var firstTimeout = dueTimeoutsAfterFirstTimeout.SingleOrDefault();
            firstTimeout.ShouldNotBe(null);
            firstTimeout.SagaId.ShouldBe(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
            firstTimeout.CorrelationId.ShouldBe("first");
            firstTimeout.ReplyTo.ShouldBe("somebody");
            firstTimeout.TimeToReturn.ShouldBe(someUtcTimeStamp);

            TimeMachine.FixTo(anotherUtcTimeStamp.AddSeconds(1));

            var dueTimeoutsAfterSecondTimeout = storage.RemoveDueTimeouts();
            var secondTimeout = dueTimeoutsAfterSecondTimeout.SingleOrDefault();
            secondTimeout.ShouldNotBe(null);
            secondTimeout.SagaId.ShouldBe(new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
            secondTimeout.CorrelationId.ShouldBe("second");
            secondTimeout.ReplyTo.ShouldBe("somebody");
            secondTimeout.TimeToReturn.ShouldBe(anotherUtcTimeStamp);
            secondTimeout.CustomData.ShouldBe(thirtytwoKilobytesOfDollarSigns);
        }

        [Test]
        public void CanRemoveMultipleTimeoutsAtOnce()
        {
            var justSomeUtcTimeStamp = new DateTime(2010, 3, 10, 12, 30, 15, DateTimeKind.Utc);

            storage.Add(new Rebus.Timeout.Timeout
            {
                CorrelationId = "first",
                ReplyTo = "somebody",
                TimeToReturn = justSomeUtcTimeStamp,
                CustomData = null,
            });

            storage.Add(new Rebus.Timeout.Timeout
            {
                CorrelationId = "second",
                ReplyTo = "somebody",
                TimeToReturn = justSomeUtcTimeStamp,
                CustomData = null,
            });

            TimeMachine.FixTo(justSomeUtcTimeStamp.AddSeconds(1));

            var dueTimeoutsAfterFirstTimeout = storage.RemoveDueTimeouts();
            dueTimeoutsAfterFirstTimeout.Count().ShouldBe(2);
        }
    }
}