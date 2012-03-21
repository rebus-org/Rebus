using System;
using System.Messaging;
using System.Text;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Persistence.InMemory;
using Rebus.Shared;
using Rebus.Transports.Msmq;
using Shouldly;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestRetryLogic : RebusBusMsmqIntegrationTestBase
    {
        protected override void DoSetUp()
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false) {MinLevel = LogLevel.Warn};
            base.DoSetUp();
        }

        [Test]
        public void CanMoveUnserializableMessageToErrorQueue()
        {
            var errorQueue = GetMessageQueue("error");

            var receiverQueueName = "test.tx.receiver";
            var receiverQueuePath = PrivateQueueNamed(receiverQueueName);
            EnsureQueueExists(receiverQueuePath);

            var messageQueueOfReceiver = new MessageQueue(receiverQueuePath);
            messageQueueOfReceiver.Formatter = new XmlMessageFormatter();
            messageQueueOfReceiver.Purge();

            CreateBus(receiverQueueName, new HandlerActivatorForTesting()).Start(1);

            messageQueueOfReceiver.Send("bla bla bla bla bla bla cannot be deserialized properly!!", MessageQueueTransactionType.Single);

            var errorMessage = (ReceivedTransportMessage)errorQueue.Receive(TimeSpan.FromSeconds(5)).Body;

            // this is how the XML formatter serializes a single string:

            // and this is the data we successfully moved to the error queue
            Encoding.UTF7.GetString(errorMessage.Body).ShouldBe("<?xml version=\"1.0\"?>\r\n<string>bla bla bla bla bla bla cannot be deserialized properly!!</string>");
        }

        [Test]
        public void CanMoveMessageToErrorQueue()
        {
            // arrange

            var retriedTooManyTimes = false;
            var senderQueueName = "test.tx.sender";
            var senderBus = CreateBus(senderQueueName, new HandlerActivatorForTesting());

            var receivedMessageCount = 0;
            var receiverQueueName = "test.tx.receiver";
            var receiverErrorQueueName = receiverQueueName + ".error";

            var errorQueue = GetMessageQueue(receiverErrorQueueName);

            CreateBus(receiverQueueName, new HandlerActivatorForTesting()
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
                                                                 }),
                      new InMemorySubscriptionStorage(),
                      new SagaDataPersisterForTesting(),
                      receiverErrorQueueName)
                .Start(1);

            senderBus.Send(receiverQueueName, "HELLO!");

            var transportMessage = (ReceivedTransportMessage)errorQueue.Receive(TimeSpan.FromSeconds(3)).Body;
            var errorMessage = serializer.Deserialize(transportMessage);

            retriedTooManyTimes.ShouldBe(false);
            errorMessage.Messages[0].ShouldBe("HELLO!");

            errorMessage.GetHeader(Headers.SourceQueue).ShouldBe(receiverQueueName);
            errorMessage.GetHeader(Headers.ErrorMessage).ShouldContain("System.Exception: oh noes!");
        }

        MessageQueue GetMessageQueue(string queueName)
        {
            var queuePath = PrivateQueueNamed(queueName);
            EnsureQueueExists(queuePath);
            var queue = new MessageQueue(queuePath)
                            {
                                MessageReadPropertyFilter = RebusTransportMessageFormatter.PropertyFilter,
                                Formatter = new RebusTransportMessageFormatter(),
                            };
            queue.Purge();
            return queue;
        }
    }
}