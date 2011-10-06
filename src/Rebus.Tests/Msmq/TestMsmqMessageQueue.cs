using System;
using System.Messaging;
using System.Transactions;
using NUnit.Framework;
using Rebus.Json;
using Rebus.Messages;
using Rebus.Msmq;

namespace Rebus.Tests.Msmq
{
    [TestFixture]
    public class TestMsmqMessageQueue : IProvideMessageTypes
    {
        MsmqMessageQueue senderQueue;
        MessageQueue destinationQueue;
        string destinationQueuePath;

        [SetUp]
        public void SetUp()
        {
            senderQueue = new MsmqMessageQueue(MsmqMessageQueue.PrivateQueue("test.msmq.tx.sender"), this, new JsonMessageSerializer());
            destinationQueuePath = MsmqMessageQueue.PrivateQueue("test.msmq.tx.destination");

            if (!MessageQueue.Exists(destinationQueuePath))
                MessageQueue.Create(destinationQueuePath, transactional: true);

            destinationQueue = new MessageQueue(destinationQueuePath)
                                   {
                                       Formatter = new RebusTransportMessageFormatter(new JsonMessageSerializer())
                                   };

            senderQueue.PurgeInputQueue();
            destinationQueue.Purge();
        }

        [Test]
        public void MessageIsSentWhenAmbientTransactionIsCommitted()
        {
            using (var tx = new TransactionScope())
            {
                senderQueue.Send(destinationQueuePath,
                                 new TransportMessage
                                     {
                                         Messages = new object[]
                                                        {
                                                            "W00t!"
                                                        }
                                     });

                tx.Complete();
            }

            var message = Receive();

            Assert.IsNotNull(message, "No message was received within timeout!");
            var transportMessage = (TransportMessage)message.Body;
            Assert.AreEqual("W00t!", transportMessage.Messages[0]);
        }

        [Test]
        public void MessageIsNotSentWhenAmbientTransactionIsNotCommitted()
        {
            using (new TransactionScope())
            {
                senderQueue.Send(destinationQueuePath,
                                 new TransportMessage
                                     {
                                         Messages = new object[]
                                                        {
                                                            "W00t! should not be delivered!"
                                                        }
                                     });

                //< we exit the scope without completing it!
            }

            var transportMessage = Receive();

            if (transportMessage != null)
            {
                Assert.Fail("No messages should have been received! ARGGH: {0}", transportMessage.Body);
            }
        }

        Message Receive()
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

        public Type[] GetMessageTypes()
        {
            return new[] { typeof(string) };
        }
    }
}