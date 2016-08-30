using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;
using Rebus.Tests.Integration.ManyMessages;

namespace Rebus.Tests.Transport.SqlServer
{
    [TestFixture]
    public class SqlServerTestManyMessages : TestManyMessages<SqlServerBusFactory> { }
}