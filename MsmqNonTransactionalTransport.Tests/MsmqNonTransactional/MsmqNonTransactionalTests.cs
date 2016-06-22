using NUnit.Framework;
using Rebus.Tests;

namespace MsmqNonTransactionalTransport.Tests.MsmqNonTransactional
{
    [TestFixture, Category(Categories.Msmq)]
    public class MsmqNonTransactionalTests
    {
        [Test]
        public void ShouldCreateNonTransactionalMsmqMessageQueue()
        {
            var factory = new MsmqNonTransactionalTransportFactory();
            var inputQueueName = TestConfig.QueueName("input");
            var queue = factory.Create(inputQueueName) as Msmq.MsmqNonTransactionalTransport;

            Assert.IsFalse(queue.IsTransactional.Value);
        }
    }
}