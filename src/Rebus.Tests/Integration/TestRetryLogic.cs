using System;
using System.Messaging;
using System.Text;
using NUnit.Framework;
using Rebus.Logging;
using Rebus.Persistence.InMemory;
using Rebus.Shared;
using Rebus.Transports.Msmq;
using Shouldly;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestRetryLogic : RebusBusMsmqIntegrationTestBase
    {
        const string SenderQueueName = "test.tx.sender";
        const string ReceiverQueueName = "test.tx.receiver";
        const string ReceiverErrorQueueName = ReceiverQueueName + ".error";

        protected override void DoSetUp()
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false) { MinLevel = LogLevel.Warn };
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
            var senderBus = CreateBus(SenderQueueName, new HandlerActivatorForTesting()).Start(1);
            var receivedMessageCount = 0;

            var errorQueue = GetMessageQueue(ReceiverErrorQueueName);

            CreateBus(ReceiverQueueName, new HandlerActivatorForTesting()
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
                      ReceiverErrorQueueName)
                .Start(1);

            senderBus.Routing.Send(ReceiverQueueName, "HELLO!");

            var transportMessage = (ReceivedTransportMessage)errorQueue.Receive(TimeSpan.FromSeconds(3)).Body;
            var errorMessage = serializer.Deserialize(transportMessage);

            retriedTooManyTimes.ShouldBe(false);
            errorMessage.Messages[0].ShouldBe("HELLO!");

            errorMessage.GetHeader(Headers.SourceQueue).ShouldBe(ReceiverQueueName + "@" + Environment.MachineName);
            errorMessage.GetHeader(Headers.ErrorMessage).ShouldContain("System.Exception: oh noes!");
        }

        [TestCase("beforeTransport")]
        [TestCase("afterTransport")]
        [TestCase("beforeLogical")]
        [TestCase("afterLogical")]
        [TestCase("poison")]
        public void CanMoveMessageToErrorQueueForExceptionsInHooks(string whenToThrow)
        {
            // arrange
            var senderBus = CreateBus(SenderQueueName, new HandlerActivatorForTesting()).Start(1);
            var errorQueue = GetMessageQueue(ReceiverErrorQueueName);

            var activator = new HandlerActivatorForTesting();
            var bus = CreateBus(ReceiverQueueName, activator,
                                new InMemorySubscriptionStorage(), new SagaDataPersisterForTesting(),
                                ReceiverErrorQueueName);

            switch (whenToThrow)
            {
                case "beforeTransport":
                    bus.Events.BeforeTransportMessage += (_, __) =>
                        {
                            throw new Exception("HELLO!");
                        };
                    break;

                case "afterTransport":
                    bus.Events.AfterTransportMessage += (_, __, ___) =>
                        {
                            throw new Exception("HELLO!");
                        };
                    break;

                case "beforeLogical":
                    bus.Events.BeforeMessage += (_, __) =>
                        {
                            throw new Exception("HELLO!");
                        };
                    break;

                case "afterLogical":
                    bus.Events.AfterMessage += (_, __, ___) =>
                        {
                            throw new Exception("HELLO!");
                        };
                    break;

                case "poison":
                    // make sure the poison event gets raised
                    activator.Handle<string>(str =>
                        {
                            throw new Exception("HELLO!");
                        });

                    bus.Events.PoisonMessage += (_, __) =>
                        {
                            throw new Exception("HELLO!");
                        };
                    break;
            }

            bus.Start(1);

            senderBus.Routing.Send(ReceiverQueueName, "HELLO!");

            var transportMessage = (ReceivedTransportMessage)errorQueue.Receive(TimeSpan.FromSeconds(3)).Body;
            var errorMessage = serializer.Deserialize(transportMessage);

            errorMessage.Messages[0].ShouldBe("HELLO!");

            errorMessage.GetHeader(Headers.SourceQueue).ShouldBe(ReceiverQueueName + "@" + Environment.MachineName);
            errorMessage.GetHeader(Headers.ErrorMessage).ShouldContain("System.Exception: HELLO!");
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