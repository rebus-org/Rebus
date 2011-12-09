// Copyright 2011 Mogens Heller Grabe
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
using NUnit.Framework;
using Rebus.Persistence.SqlServer;
using Rebus.Tests.Persistence.SqlServer;

namespace Rebus.Tests.Performance
{
    [TestFixture, Category(TestCategories.MsSql), Category(TestCategories.Performance)]
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
            SagaPersisterPerformanceTestHelper.DoTheTest(persister, numberOfSagas, iterations);
        }
    }
}