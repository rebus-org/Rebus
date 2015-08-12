using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.AmazonSQS.Tests
{
    [TestFixture, Category(Category.AmazonSqs)]
    public class AmazonSqsSimpleSend : BasicSendReceive<AmazonSQSTransportFactory> { }
}
