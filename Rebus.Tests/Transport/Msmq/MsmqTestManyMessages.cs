using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;
using Rebus.Tests.Integration.ManyMessages;

namespace Rebus.Tests.Transport.Msmq
{
    [TestFixture]
    public class MsmqTestManyMessages : TestManyMessages<MsmqBusFactory> { }
}