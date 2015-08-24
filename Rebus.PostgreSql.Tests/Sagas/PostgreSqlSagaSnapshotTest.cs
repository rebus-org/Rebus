using NUnit.Framework;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.PostgreSql.Tests.Sagas
{
    [TestFixture]
    public class PostgreSqlSagaSnapshotTest : SagaSnapshotStorageTest<PostgreSqlSnapshotStorageFactory> { }
}