using System;
using System.Messaging;
using System.Threading;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Serialization.Json;
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

            var errorMessage = (string)errorQueue.Receive(TimeSpan.FromSeconds(5)).Body;
            Assert.AreEqual("bla bla bla bla bla bla cannot be deserialized properly!!", errorMessage);
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

            var errorMessage = (TransportMessage)errorQueue.Receive(TimeSpan.FromSeconds(5)).Body;

            Assert.IsFalse(retriedTooManyTimes, "Apparently, the message was delivered more than 5 times which is the default number of retries");
            Assert.AreEqual("HELLO!", errorMessage.Messages[0]);
            Assert.IsTrue(errorMessage.Headers["errorMessage"].Contains("oh noes!"));
        }

        MessageQueue GetMessageQueue(string queueName)
        {
            var errorQueueName = PrivateQueueNamed(queueName);
            EnsureQueueExists(errorQueueName);
            var errorQueue = new MessageQueue(errorQueueName);
            errorQueue.Formatter = new RebusTransportMessageFormatter(new JsonMessageSerializer());
            errorQueue.Purge();
            return errorQueue;
        }
    }
}