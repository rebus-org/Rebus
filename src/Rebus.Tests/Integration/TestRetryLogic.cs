using System;
using System.Messaging;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Transports.Msmq;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestRetryLogic : RebusBusMsmqIntegrationTestBase
    {
        [Test]
        public void CanMoveUnserializableMessageToErrorQueue()
        {
            var errorQueue = GetMessageQueue("error");

            var receiverQueueName = PrivateQueueNamed("test.tx.receiver");
            EnsureQueueExists(receiverQueueName);

            var messageQueueOfReceiver = new MessageQueue(receiverQueueName);
            messageQueueOfReceiver.Formatter = new XmlMessageFormatter();
            messageQueueOfReceiver.Purge();

            CreateBus(receiverQueueName, new HandlerActivatorForTesting()).Start();

            messageQueueOfReceiver.Send("bla bla bla bla bla bla cannot be deserialized properly!!", MessageQueueTransactionType.Single);

            var errorMessage = (TransportMessage)errorQueue.Receive(TimeSpan.FromSeconds(5)).Body;
            
            // this is how the XML formatter serializes a single string:
            var expected = "<?xml version=\"1.0\"?>\r\n<string>bla bla bla bla bla bla cannot be deserialized properly!!</string>";
            
            // and this is the data we successfully moved to the error queue
            var actual = Encoding.UTF8.GetString(errorMessage.Data);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void CanMoveMessageToErrorQueue()
        {
            // arrange
            var errorQueue = GetMessageQueue("error");

            var retriedTooManyTimes = false;
            var senderQueueName = PrivateQueueNamed("test.tx.sender");
            var senderBus = CreateBus(senderQueueName, new HandlerActivatorForTesting());

            var resetEvent = new ManualResetEvent(false);
            var receivedMessageCount = 0;
            var receiverQueueName = PrivateQueueNamed("test.tx.receiver");
            CreateBus(receiverQueueName,
                      new HandlerActivatorForTesting()
                          .Handle<string>(str =>
                                              {
                                                  if (str != "HELLO!") return;

                                                  receivedMessageCount++;

                                                  if (receivedMessageCount > 5)
                                                  {
                                                      retriedTooManyTimes = true;
                                                      resetEvent.Set();
                                                  }
                                                  else
                                                  {
                                                      throw new Exception("oh noes!");
                                                  }
                                              }))
                .Start();

            senderBus.Send(receiverQueueName, "HELLO!");

            var transportMessage = (TransportMessage)errorQueue.Receive(TimeSpan.FromSeconds(5)).Body;
            var errorMessage = serializer.Deserialize(transportMessage);

            Assert.IsFalse(retriedTooManyTimes, "Apparently, the message was delivered more than 5 times which is the default number of retries");
            Assert.AreEqual("HELLO!", errorMessage.Messages[0]);
        }

        MessageQueue GetMessageQueue(string queueName)
        {
            var errorQueueName = PrivateQueueNamed(queueName);
            EnsureQueueExists(errorQueueName);
            var errorQueue = new MessageQueue(errorQueueName);
            errorQueue.Formatter = new RebusTransportMessageFormatter();
            errorQueue.Purge();
            return errorQueue;
        }
    }
}