using NUnit.Framework;
using Rebus.Tests.Persistence.SqlServer;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestSagaCorrelationSql : TestSagaCorrelation<SqlServerSagaStorageFactory> { }
}