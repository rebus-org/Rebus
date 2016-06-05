using System;
using System.Collections.Generic;
using System.Messaging;
using System.Threading.Tasks;
using MsmqNonTransactionalTransport.Tests.Contracts.Transports;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Tests;
using Rebus.Tests.Extensions;

namespace MsmqNonTransactionalTransport.Tests.MsmqNonTransactional
{
    [TestFixture, Category(Categories.Msmq)]
    public class MsmqNonTransactionalBasicSendReceive : BasicSendReceive<MsmqNonTransactionalTransportFactory>
    {
        /// <summary>
        /// send 2 messages with one having higher prio and confirm the message with higher prio is processed first
        /// Don't know whether this test should be here or in BasicSendReceive (because of other transports)
        /// </summary>
        [Test]
        public async Task MsmqMessagePriorityCheck()
        {
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
            message.Headers.TryGetValue(Headers.Priority, out messagePriority);

            Assert.AreEqual(MessagePriority.High.ToString(), messagePriority);

            message = await input2.AwaitReceive();
            message.Headers.TryGetValue(Headers.Priority, out messagePriority);

            Assert.AreEqual(MessagePriority.Low.ToString(), messagePriority);
        }

        protected TransportMessage MessageWith(string stringBody, MessagePriority priority = MessagePriority.Normal)
        {
            var headers = new Dictionary<string, string>
            {
                {Headers.MessageId, Guid.NewGuid().ToString()},
                {Headers.Priority, priority.ToString() }

            };
            var body = _defaultEncoding.GetBytes(stringBody);
            return new TransportMessage(headers, body);
        }
    }
}