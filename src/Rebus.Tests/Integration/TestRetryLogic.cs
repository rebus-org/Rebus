using System;
using System.Messaging;
using System.Text;
using System.Transactions;
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
        [TestCase("commitHook", Ignore = true)]
        [TestCase("rollbackHook", Ignore = true)]
        [TestCase("prepareHook", Ignore = true)]
        [TestCase("inDoubtHook", Ignore = true)]
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

                    bus.Events.PoisonMessage += (_, __, ___) =>
                        {
                            throw new Exception("HELLO!");
                        };
                    break;

                case "commitHook":
                    activator.Handle<string>(str => Transaction.Current
                                                        .EnlistVolatile(new ThingToEnlistThatWillFailOn(commit: true),
                                                                       EnlistmentOptions.None));
                    break;

                case "rollbackHook":
                    activator.Handle<string>(str =>
                        {
                            Transaction.Current
                                .EnlistVolatile(new ThingToEnlistThatWillFailOn(rollback: true),
                                                EnlistmentOptions.None);

                            throw new Exception("HELLO!");
                        });
                    break;

                case "prepareHook":
                    activator.Handle<string>(str => Transaction.Current
                                                        .EnlistVolatile(new ThingToEnlistThatWillFailOn(prepare: true),
                                                                       EnlistmentOptions.None));
                    break;

                case "inDoubtHook":
                    activator.Handle<string>(str => Transaction.Current
                                                        .EnlistVolatile(new ThingToEnlistThatWillFailOn(inDoubt: true),
                                                                       EnlistmentOptions.None));
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

    public class ThingToEnlistThatWillFailOn : IEnlistmentNotification
    {
        readonly bool prepare;
        readonly bool commit;
        readonly bool rollback;
        readonly bool inDoubt;

        public ThingToEnlistThatWillFailOn(bool prepare = false, bool commit = false, bool rollback = false, bool inDoubt = false)
        {
            this.prepare = prepare;
            this.commit = commit;
            this.rollback = rollback;
            this.inDoubt = inDoubt;
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            Console.WriteLine("preparing...");
            if (prepare) throw new OmfgExceptionThisIsBad("prepareHook");
            preparingEnlistment.Prepared();
            Console.WriteLine("done preparing!");
        }

        public void Commit(Enlistment enlistment)
        {
            Console.WriteLine("committing...");
            if (commit) throw new OmfgExceptionThisIsBad("commitHook");
            enlistment.Done();
            Console.WriteLine("done committing!");
        }

        public void Rollback(Enlistment enlistment)
        {
            Console.WriteLine("rollbacking...");
            if (rollback) throw new OmfgExceptionThisIsBad("rollbackHook");
            enlistment.Done();
            Console.WriteLine("done rollbacking!");
        }

        public void InDoubt(Enlistment enlistment)
        {
            Console.WriteLine("indoubting...");
            if (inDoubt) throw new OmfgExceptionThisIsBad("inDoubtHook");
            enlistment.Done();
            Console.WriteLine("done indoubting!");
        }
    }

    public class OmfgExceptionThisIsBad : Exception
    {
        public OmfgExceptionThisIsBad(string message)
            : base(message)
        {
        }
    }
}