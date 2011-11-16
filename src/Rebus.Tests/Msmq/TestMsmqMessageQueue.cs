using System;
using System.Collections.Generic;
using System.Messaging;
using System.Threading;
using System.Transactions;
using NUnit.Framework;
using Rebus.Serialization.Json;
using Rebus.Transports.Msmq;
using Message = Rebus.Messages.Message;

namespace Rebus.Tests.Msmq
{
    [TestFixture]
    public class TestMsmqMessageQueue
    {
        MsmqMessageQueue senderQueue;
        MessageQueue destinationQueue;
        string destinationQueuePath;
        JsonMessageSerializer serializer;

        [SetUp]
        public void SetUp()
        {
            serializer = new JsonMessageSerializer();
            senderQueue = new MsmqMessageQueue(MsmqMessageQueue.PrivateQueue("test.msmq.tx.sender"));
            destinationQueuePath = MsmqMessageQueue.PrivateQueue("test.msmq.tx.destination");

            if (!MessageQueue.Exists(destinationQueuePath))
                MessageQueue.Create(destinationQueuePath, transactional: true);

            destinationQueue = new MessageQueue(destinationQueuePath)
                                   {
                                       Formatter = new RebusTransportMessageFormatter()
                                   };

            senderQueue.PurgeInputQueue();
            destinationQueue.Purge();
        }

        [Test]
        public void MessageExpirationWorks()
        {
            // arrange
            var timeToBeReceived = 2.Seconds();
            var timeToBeReceivedAsString = timeToBeReceived.ToString();
            
            senderQueue.Send(destinationQueuePath,
                             serializer.Serialize(new Message
                                                      {
                                                          Messages = new object[] {"HELLO WORLD!"},
                                                          Headers =
                                                              new Dictionary<string, string>
                                                                  {{"TimeToBeReceived", timeToBeReceivedAsString}},
                                                      }));

            // act
            Thread.Sleep(timeToBeReceived + 1.Seconds());

            // assert
            Assert.Throws<MessageQueueException>(() => destinationQueue.Receive(0.1.Seconds()));
        }

        [Test]
        public void MessageIsSentWhenAmbientTransactionIsCommitted()
        {
            using (var tx = new TransactionScope())
            {
                senderQueue.Send(destinationQueuePath,
                                 serializer.Serialize(new Message
                                                          {
                                                              Messages = new object[]
                                                                             {
                                                                                 "W00t!"
                                                                             }
                                                          }));

                tx.Complete();
            }

            var msmqMessage = Receive();

            Assert.IsNotNull(msmqMessage, "No message was received within timeout!");
            var transportMessage = (ReceivedTransportMessage)msmqMessage.Body;
            var message = serializer.Deserialize(transportMessage);
            Assert.AreEqual("W00t!", message.Messages[0]);
        }

        [Test]
        public void MessageIsNotSentWhenAmbientTransactionIsNotCommitted()
        {
            using (new TransactionScope())
            {
                senderQueue.Send(destinationQueuePath,
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
            catch(MessageQueueException)
            {
                return null;
            }
        }
    }
}