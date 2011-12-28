using System;
using System.Messaging;
using NUnit.Framework;
using Rebus.Transports.Msmq;
using Shouldly;

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

            var errorMessage = (ReceivedTransportMessage)errorQueue.Receive(TimeSpan.FromSeconds(5)).Body;
            
            // this is how the XML formatter serializes a single string:

            // and this is the data we successfully moved to the error queue
            errorMessage.Data.ShouldBe("<?xml version=\"1.0\"?>\r\n<string>bla bla bla bla bla bla cannot be deserialized properly!!</string>");
        }

        [Test]
        public void CanMoveMessageToErrorQueue()
        {
            // arrange
            var errorQueue = GetMessageQueue("error");

            var retriedTooManyTimes = false;
            var senderQueueName = PrivateQueueNamed("test.tx.sender");
            var senderBus = CreateBus(senderQueueName, new HandlerActivatorForTesting());

            var receivedMessageCount = 0;
            var receiverQueueName = PrivateQueueNamed("test.tx.receiver");
            CreateBus(receiverQueueName,
                      new HandlerActivatorForTesting()
                          .Handle<string>(str =>
                                              {
                                                  Console.WriteLine("Delivery!");
                                                  if (str != "HELLO!") return;

                                                  receivedMessageCount++;

                                                  if (receivedMessageCount > 5)
                                                  {
                                                      retriedTooManyTimes = true;
                                                  }
                                                  else
                                                  {
                                                      throw new Exception("oh noes!");
                                                  }
                                              }))
                .Start();

            senderBus.Send(receiverQueueName, "HELLO!");

            var transportMessage = (ReceivedTransportMessage)errorQueue.Receive(TimeSpan.FromSeconds(2)).Body;
            var errorMessage = serializer.Deserialize(transportMessage);

            retriedTooManyTimes.ShouldBe(false);
            errorMessage.Messages[0].ShouldBe("HELLO!");
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