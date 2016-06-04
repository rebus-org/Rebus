using NUnit.Framework;
using Rebus.Tests.Contracts.Locks;

namespace Rebus.Tests.Persistence.SqlServer
{
    [TestFixture]
    public class SqlServerPessimisticLocksTest : PessimisticLocksTest<SqlServerPessimisticLockerFactory>
    {
    }
}