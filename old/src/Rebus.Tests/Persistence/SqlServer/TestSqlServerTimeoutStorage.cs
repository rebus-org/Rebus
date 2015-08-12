using System;
using System.Linq;
using NUnit.Framework;
using Rebus.Persistence.SqlServer;
using Shouldly;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture, Category(TestCategories.MsSql)]
    public class TestSqlServerTimeoutStorage : SqlServerFixtureBase
    {
        SqlServerTimeoutStorage storage;
        const string TimeoutsTableName = "timeouts";

        protected override void DoSetUp()
        {
            // ensure the two tables are dropped
            DropTable(TimeoutsTableName);

            storage = new SqlServerTimeoutStorage(ConnectionStrings.SqlServer, TimeoutsTableName);
        }

        [Test, Description("Verifies that competing timeout consumers do not get the same timeouts")]
        public void ExertsRowLevelLockingBecauseItCan()
        {
            storage.EnsureTableIsCreated();

            var now = DateTime.UtcNow;
            
            var t1 = now;
            var t2 = now.AddDays(1);

            storage.Add(new Rebus.Timeout.Timeout("bimse", "1", t1, Guid.Empty, "hej"));
            storage.Add(new Rebus.Timeout.Timeout("bimse", "2", t2, Guid.Empty, "hej"));

            TimeMachine.FixTo(t1.AddMinutes(-10));

            using (var resultBefore = storage.GetDueTimeouts())
            {
                var timeoutsBefore = resultBefore.DueTimeouts.ToList();

                TimeMachine.FixTo(t1.AddMinutes(10));

                using (var resultsAfterT1 = storage.GetDueTimeouts())
                {
                    var timeoutsAfterT1 = resultsAfterT1.DueTimeouts.ToList();

                    TimeMachine.FixTo(t2.AddMinutes(10));

                    using (var resultsAfterT2 = storage.GetDueTimeouts())
                    {
                        var timeoutsAfterT2 = resultsAfterT2.DueTimeouts.ToList();

                        Assert.That(timeoutsBefore.Count, Is.EqualTo(0));
                        Assert.That(timeoutsAfterT1.Count, Is.EqualTo(1));
                        Assert.That(timeoutsAfterT2.Count, Is.EqualTo(1));
                    }
                }
            }
        }

        [Test]
        public void CanCreateStorageTableAutomatically()
        {
            // arrange

            // act
            storage.EnsureTableIsCreated();

            // assert
            var tableNames = GetTableNames();
            tableNames.ShouldContain(TimeoutsTableName);
        }

        [Test]
        public void DoesntDoAnythingIfTheTableAlreadyExists()
        {
            // arrange
            ExecuteCommand("create table " + TimeoutsTableName + "(id int not null)");

            // act
            // assert
            storage.EnsureTableIsCreated();
            storage.EnsureTableIsCreated();
            storage.EnsureTableIsCreated();
        }
    }
}