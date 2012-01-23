using System;
using NUnit.Framework;
using Rebus.Timeout;
using System.Linq;
using Shouldly;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture, Category(TestCategories.MsSql)]
    public class TestSqlServerTimeoutStorage : DbFixtureBase
    {
        SqlServerTimeoutStorage storage;

        protected override void DoSetUp()
        {
            storage = new SqlServerTimeoutStorage(ConnectionString, "timeouts");
        }

        [Test]
        public void DoesNotComplainWhenTheSameTimeoutIsAddedMultipleTimes()
        {
            var justSomeTime = new DateTime(2010, 1, 1, 10, 30, 0, DateTimeKind.Utc);

            storage.Add(new Timeout.Timeout{CorrelationId="blah", ReplyTo = "blah blah", TimeToReturn = justSomeTime});
            storage.Add(new Timeout.Timeout{CorrelationId="blah", ReplyTo = "blah blah", TimeToReturn = justSomeTime});
            storage.Add(new Timeout.Timeout{CorrelationId="blah", ReplyTo = "blah blah", TimeToReturn = justSomeTime});
        }

        [Test]
        public void CanStoreAndRemoveTimeouts()
        {
            var justSomeUtcTimeStamp = new DateTime(2010, 3, 10, 12, 30, 15, DateTimeKind.Utc);
            var justAnotherUtcTimeStamp = justSomeUtcTimeStamp.AddHours(2);

            storage.Add(new Timeout.Timeout
                            {
                                CorrelationId = "first",
                                ReplyTo = "somebody",
                                TimeToReturn = justSomeUtcTimeStamp
                            });

            storage.Add(new Timeout.Timeout
                            {
                                CorrelationId = "second",
                                ReplyTo = "somebody",
                                TimeToReturn = justAnotherUtcTimeStamp
                            });

            TimeMachine.FixTo(justSomeUtcTimeStamp.AddSeconds(-1));

            var dueTimeoutsBeforeTimeout = storage.RemoveDueTimeouts().Count();
            dueTimeoutsBeforeTimeout.ShouldBe(0);

            TimeMachine.FixTo(justSomeUtcTimeStamp.AddSeconds(1));

            var dueTimeoutsAfterFirstTimeout = storage.RemoveDueTimeouts();
            dueTimeoutsAfterFirstTimeout.Count().ShouldBe(1);
            
            var timeout = dueTimeoutsAfterFirstTimeout.First();
            timeout.CorrelationId.ShouldBe("first");
            timeout.ReplyTo.ShouldBe("somebody");
            timeout.TimeToReturn.ShouldBe(justSomeUtcTimeStamp);

            TimeMachine.FixTo(justAnotherUtcTimeStamp.AddSeconds(1));

            var dueTimeoutsAfterSecondTimeout = storage.RemoveDueTimeouts();
            dueTimeoutsAfterSecondTimeout.Count().ShouldBe(1);

            var secondTimeout = dueTimeoutsAfterSecondTimeout.First();
            secondTimeout.CorrelationId.ShouldBe("second");
            secondTimeout.ReplyTo.ShouldBe("somebody");
            secondTimeout.TimeToReturn.ShouldBe(justAnotherUtcTimeStamp);
        }
    }
}