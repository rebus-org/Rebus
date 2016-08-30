using NUnit.Framework;
using Rebus.Tests.Contracts.DataBus;

namespace Rebus.Tests.DataBus.SqlServer
{
    [TestFixture]
    public class SqlServerDataBusStorageTest : GeneralDataBusStorageTests<SqlServerDataBusStorageFactory> { }
}