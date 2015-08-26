using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.Tests.Transport.Msmq
{
    [TestFixture, Category(Categories.Msmq)]
    public class MsmqBasicSendReceive : BasicSendReceive<MsmqTransportFactory> { }
}