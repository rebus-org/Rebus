using MsmqNonTransactionalTransport.Tests.Contracts.Transports;
using NUnit.Framework;
using Rebus.Tests;
using Rebus.Tests.Transport.Msmq;

namespace MsmqNonTransactionalTransport.Tests
{
    [TestFixture, Category(Categories.Msmq)]
    public class MsmqMessageExpiration : MessageExpiration<MsmqTransportFactory> { }
}