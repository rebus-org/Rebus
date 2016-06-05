using System;
using System.Collections.Generic;
using System.Messaging;
using System.Threading.Tasks;
using MsmqNonTransactionalTransport.Msmq;
using MsmqNonTransactionalTransport.Tests.Contracts.Transports;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Tests;
using Rebus.Tests.Extensions;

namespace MsmqNonTransactionalTransport.Tests
{
    [TestFixture, Category(Categories.Msmq)]
    public class MsmqBasicSendReceive : BasicSendReceive<MsmqTransportFactory>
    {
        [Test]
        public void ShouldCreateTransactionalMsmqMessageQueue()
        {
            var inputQueueName = TestConfig.QueueName("input");
            var queue = _factory.Create(inputQueueName) as MsmqTransport;
            Assert.IsTrue(queue.IsTransactional.Value);
        }
    }

    [TestFixture, Category(Categories.Msmq)]
    public class MsmqNonTransactionalBasicSendReceive : BasicSendReceive<MsmqNonTransactionalTransportFactory>
    {
        // const copied from SQL transport
        const string MessagePriorityHeaderKey = "rbs2-msg-priority";

        /// <summary>
        /// Don't know whether this test should be here or in BasicSendReceive (because of other transports)
        /// </summary>
        [Test]
        public async Task MsmqMessagePriorityCheck()
        {
            // send 2 messages with one having higher prio and confirm the message with higher prio is processed first
            var input1QueueName = TestConfig.QueueName("input1");
            var input2QueueName = TestConfig.QueueName("input2");

            var input1 = _factory.Create(input1QueueName);
            var input2 = _factory.Create(input2QueueName);

            await WithContext(async context =>
            {
                await input1.Send(input2QueueName, MessageWith("low prio", MessagePriority.Low), context);
                await input1.Send(input2QueueName, MessageWith("high prio", MessagePriority.High), context);
            });

            var message = await input2.AwaitReceive();
            string messagePriority;
            message.Headers.TryGetValue(MessagePriorityHeaderKey, out messagePriority);

            Assert.AreEqual(MessagePriority.High.ToString(), messagePriority);
            //            await WithContext(async context =>
            //            {
            //                var transportMessage = await input2.Receive(context);
            //                var stringBody = GetStringBody(transportMessage);
            //
            //                Assert.That(stringBody, Is.EqualTo("high prio"));
            //            });
        }

        protected TransportMessage MessageWith(string stringBody, MessagePriority priority = MessagePriority.Normal)
        {
            var headers = new Dictionary<string, string>
            {
                {Headers.MessageId, Guid.NewGuid().ToString()},
                {MessagePriorityHeaderKey, priority.ToString() }

            };
            var body = _defaultEncoding.GetBytes(stringBody);
            return new TransportMessage(headers, body);
        }
    }

    [TestFixture, Category(Categories.Msmq)]
    public class MsmqNonTransactionalTests
    {
        readonly MsmqNonTransactionalTransportFactory _factory = new MsmqNonTransactionalTransportFactory();

        [Test]
        public void ShouldCreateNonTransactionalMsmqMessageQueue()
        {
            var inputQueueName = TestConfig.QueueName("input");
            var queue = _factory.Create(inputQueueName) as MsmqTransport;
            Assert.IsFalse(queue.IsTransactional.Value);
        }
    }
}