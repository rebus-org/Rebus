using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Messaging;
using System.Text;
using System.Threading;
using System.Transactions;
using NUnit.Framework;
using Rebus.Serialization;
using Rebus.Serialization.Json;
using Rebus.Shared;
using Rebus.Transports.Msmq;
using Shouldly;
using Message = Rebus.Messages.Message;

namespace Rebus.Tests.Transports.Msmq
{
    [TestFixture]
    public class TestMsmqMessageQueue
    {
        MsmqMessageQueue senderQueue;
        MessageQueue destinationQueue;
        string destinationQueuePath;
        JsonMessageSerializer serializer;
        string destinationQueueName;

        [SetUp]
        public void SetUp()
        {
            serializer = new JsonMessageSerializer();
            senderQueue = new MsmqMessageQueue("test.msmq.tx.sender", "error");
            destinationQueueName = "test.msmq.tx.destination";
            destinationQueuePath = MsmqMessageQueue.PrivateQueue(destinationQueueName);

            if (!MessageQueue.Exists(destinationQueuePath))
            {
                var messageQueue = MessageQueue.Create(destinationQueuePath, transactional: true);
                messageQueue.SetPermissions(Thread.CurrentPrincipal.Identity.Name, MessageQueueAccessRights.FullControl);
            }

            destinationQueue = new MessageQueue(destinationQueuePath)
                                   {
                                       Formatter = new RebusTransportMessageFormatter(),
                                       MessageReadPropertyFilter = RebusTransportMessageFormatter.PropertyFilter,
                                   };

            senderQueue.PurgeInputQueue();
            destinationQueue.Purge();
        }

        /// <summary>
        /// Before refactoring:
        ///     Sending 10000 messages took 7 s - that's 1427 msg/s
        ///
        /// After refactoring:
        ///     Sending 10000 messages took 29 s - that's 340 msg/s
        /// 
        /// On battery, in the train:
        ///     Sending 10000 messages took 32 s - that's 312 msg/s
        /// </summary>
        [TestCase(10000)]
        public void CheckSendPerformance(int count)
        {
            var queue = new MsmqMessageQueue("test.msmq.performance", "error").PurgeInputQueue();
            var transportMessageToSend = new TransportMessageToSend
                                             {
                                                 Headers = new Dictionary<string, string>(),
                                                 Body = new byte[1024],
                                                 Label = "this is just a label"
                                             };

            var stopwatch = Stopwatch.StartNew();
            count.Times(() => queue.Send("test.msmq.performance", transportMessageToSend));
            var totalSeconds = stopwatch.Elapsed.TotalSeconds;

            Console.WriteLine("Sending {0} messages took {1:0} s - that's {2:0} msg/s",
                              count, totalSeconds, count/totalSeconds);
        }

        [Test]
        public void ThrowsIfExistingQueueIsNotTransactional()
        {
            // arrange
            var queueName = "test.some.random.queue";
            var queuePath = MsmqMessageQueue.PrivateQueue(queueName);

            if (MessageQueue.Exists(queuePath))
            {
                MessageQueue.Delete(queuePath);
            }

            MessageQueue.Create(queuePath, transactional: false);

            // act
            var invalidOperationException = Assert.Throws<InvalidOperationException>(() => new MsmqMessageQueue(queueName, "error"));

            // assert
            invalidOperationException.Message.ShouldContain(queueName);
        }

        [Test]
        public void MessageExpirationWorks()
        {
            // arrange
            var timeToBeReceived = 2.Seconds()
                .ToString();

            senderQueue.Send(destinationQueueName,
                             serializer.Serialize(new Message
                                                      {
                                                          Messages = new object[] { "HELLO WORLD!" },
                                                          Headers = new Dictionary<string, string> { { Headers.TimeToBeReceived, timeToBeReceived } },
                                                      }));

            // act
            Thread.Sleep(2.Seconds() + 1.Seconds());

            // assert
            Assert.Throws<MessageQueueException>(() => destinationQueue.Receive(0.1.Seconds()));
        }

        [Test]
        public void MessageIsSentWhenAmbientTransactionIsCommitted()
        {
            using (var tx = new TransactionScope())
            {
                senderQueue.Send(destinationQueueName,
                                 serializer.Serialize(new Message
                                                          {
                                                              Messages = new object[]
                                                                             {
                                                                                 "W00t!"
                                                                             },
                                                          }));

                tx.Complete();
            }

            var msmqMessage = Receive();

            Assert.IsNotNull(msmqMessage, "No message was received within timeout!");
            var transportMessage = (ReceivedTransportMessage)msmqMessage.Body;
            var message = serializer.Deserialize(transportMessage);
            message.Messages[0].ShouldBe("W00t!");
        }

        [Test]
        public void HeadersAreTransferred()
        {
            var headers = new Dictionary<string, string>
                              {
                                  {"someRandomHeaderKey", "someRandomHeaderValue"},
                              };

            senderQueue.Send(destinationQueueName,
                             serializer.Serialize(new Message
                                                      {
                                                          Messages = new object[] {"W00t!"},
                                                          Headers = headers
                                                      }));
            var msmqMessage = Receive();

            Assert.IsNotNull(msmqMessage, "No message was received within timeout!");
            
            var receivedTransportMessage = (ReceivedTransportMessage)msmqMessage.Body;
            receivedTransportMessage.Headers = new DictionarySerializer().Deserialize(Encoding.UTF7.GetString(msmqMessage.Extension));
            var message = serializer.Deserialize(receivedTransportMessage);

            message.Headers.ShouldNotBe(null);
            message.Headers.ShouldContainKeyAndValue("someRandomHeaderKey", "someRandomHeaderValue");
        }

        [Test]
        public void MessageIsNotSentWhenAmbientTransactionIsNotCommitted()
        {
            using (new TransactionScope())
            {
                senderQueue.Send(destinationQueueName,
                                 serializer.Serialize(new Message
                                                          {
                                                              Messages = new object[]
                                                                             {
                                                                                 "W00t! should not be delivered!"
                                                                             }
                                                          }));

                //< we exit the scope without completing it!
            }

            var transportMessage = Receive();

            if (transportMessage != null)
            {
                Assert.Fail("No messages should have been received! ARGGH: {0}", transportMessage.Body);
            }
        }

        System.Messaging.Message Receive()
        {
            try
            {
                return destinationQueue.Receive(TimeSpan.FromSeconds(5));
            }
            catch (MessageQueueException)
            {
                return null;
            }
        }
    }
}