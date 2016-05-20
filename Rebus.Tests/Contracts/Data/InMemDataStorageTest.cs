using NUnit.Framework;

namespace Rebus.Tests.Contracts.Data
{
    [TestFixture]
    public class InMemDataStorageTest : GeneralDataStorageTests<InMemDataBusStorageFactory> { }
}