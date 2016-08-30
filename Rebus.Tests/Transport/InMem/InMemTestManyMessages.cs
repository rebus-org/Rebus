using NUnit.Framework;

namespace Rebus.Tests.Integration.ManyMessages
{
    [TestFixture]
    public class InMemTestManyMessages : TestManyMessages<InMemoryBusFactory> { }
}