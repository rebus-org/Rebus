using NUnit.Framework;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.PostgreSql.Tests.Sagas
{
    [TestFixture, Category(TestCategory.Postgres)]
    public class PostgreSqlSagaSnapshotTest : SagaSnapshotStorageTest<PostgreSqlSnapshotStorageFactory> { }
}