using NUnit.Framework;
using Rebus.Persistence.SqlServer;
using Rebus.Tests.Persistence.SqlServer;

namespace Rebus.Tests.Performance
{
    [TestFixture, Category(TestCategories.MsSql)]
    public class TestSqlServerSagaPersisterPerformance : DbFixtureBase
    {
        protected IStoreSagaData persister;

        protected override void DoSetUp()
        {
            persister = new SqlServerSagaPersister(ConnectionString, "saga_index", "sagas");
            DeleteRows("sagas");
            DeleteRows("saga_index");
        }

        /// <summary>
        /// Initial:
        ///     Saving/updating 100 sagas 10 times took 10,6 s - that's 94 ops/s
        ///     Saving/updating 1000 sagas 2 times took 10,6 s - that's 95 ops/s
        /// 
        /// Combined delete index/insert into sagas:
        ///     Saving/updating 100 sagas 10 times took 10,1 s - that's 99 ops/s
        ///     Saving/updating 1000 sagas 2 times took 10,1 s - that's ? ops/s
        ///
        /// Switched insert/update order, making update the most anticipated scenario and insert the least:
        ///     Saving/updating 100 sagas 10 times took 9,0 s - that's 111 ops/s
        ///     Saving/updating 1000 sagas 2 times took 22,3 s - that's 90 ops/s
        /// </summary>
        [TestCase(100, 10)]
        [TestCase(1000, 2)]
        public void RunTest(int numberOfSagas, int iterations)
        {
            SagaPersisterPerformanceFixtureBase.DoTheTest(persister, numberOfSagas, iterations);
        }
    }
}