using NUnit.Framework;
using Rebus.Tests;
using Rebus.Tests.Contracts.Transports;

namespace MsmqNonTransactionalTransport.Tests
{
    [TestFixture, Category(Categories.Msmq)]
    public class MsmqMessageExpiration : MessageExpiration<MsmqTransportFactory> { }
}