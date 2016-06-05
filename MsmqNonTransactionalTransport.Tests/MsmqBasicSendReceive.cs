using MsmqNonTransactionalTransport.Tests.Contracts.Transports;
using NUnit.Framework;
using Rebus.Tests;
using Rebus.Tests.Transport.Msmq;
using Rebus.Transport.Msmq;

namespace MsmqNonTransactionalTransport.Tests
{
    [TestFixture, Category(Categories.Msmq)]
    public class MsmqBasicSendReceive : BasicSendReceive<MsmqTransportFactory>
    {
        [Test]
        public void ShouldCreateTransactionalMsmqMessageQueue()
        {
            var inputQueueName = TestConfig.QueueName("input");
            MsmqUtil.Delete(inputQueueName);
            var queue = _factory.Create(inputQueueName) as MsmqTransport;
            Assert.IsTrue(queue.IsTransactional.Value);
        }
    }
}