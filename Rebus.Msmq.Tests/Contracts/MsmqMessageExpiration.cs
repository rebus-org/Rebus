using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.Msmq.Tests.Contracts
{
    [TestFixture, Category(Categories.Msmq)]
    public class MsmqMessageExpiration : MessageExpiration<MsmqTransportFactory> { }
}