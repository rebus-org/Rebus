using MsmqNonTransactionalTransport.Tests.Contracts.Transports;
using NUnit.Framework;
using Rebus.Tests;

namespace MsmqNonTransactionalTransport.Tests.MsmqNonTransactional
{
    [TestFixture, Category(Categories.Msmq)]
    public class MsmqNonTransactionalMessageExpiration : MessageExpiration<MsmqNonTransactionalTransportFactory> { }
}