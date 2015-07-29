using System;
using System.Threading;
using NUnit.Framework;
using Raven.Abstractions.Extensions;
using Rebus.Shared;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(TestCategories.Integration)]
    public class TestRebusBusWithMsmqMessageQueue : RebusBusMsmqIntegrationTestBase
    {
        const string SenderInputQueueName = "test.tx.sender";
        const string ReceiverInputQueueName = "test.tx.receiver";
        const string RecipientQueueName = "test.recipient";
        const string AnotherSenderInputQueueName = "test.sender";
        const string RequestorQueueName = "test.requestor";
        const string ReplierQueueName = "test.replier";
        const string PublisherInputQueue = "test.publisher";
        const string FirstSubscriberInputQueue = "test.subscriber1";
        const string SecondSubscriberInputQueue = "test.subscriber2";

        static readonly string[] Queues =
        {
            SenderInputQueueName,
            ReceiverInputQueueName,
            RecipientQueueName,
            AnotherSenderInputQueueName,
            RequestorQueueName,
            ReplierQueueName,
            PublisherInputQueue,
            FirstSubscriberInputQueue,
            SecondSubscriberInputQueue
        };

        protected override void DoSetUp()
        {
            Queues.ForEach(MsmqUtil.Delete);
        }

        protected override void DoTearDown()
        {
            Queues.ForEach(MsmqUtil.Delete);
        }

        [Test]
        public void CanReceiveMessagesInTransaction()
        {
            // arrange
            var senderBus = CreateBus(SenderInputQueueName, new HandlerActivatorForTesting()).Start(1);

            var resetEvent = new ManualResetEvent(false);
            var receivedMessageCount = 0;

            CreateBus(ReceiverInputQueueName,
                      new HandlerActivatorForTesting()
                          .Handle<string>(str =>
                                              {
                                                  if (str != "HELLO!") return;

                                                  receivedMessageCount++;

                                                  // throw the first two times the message is delivered
                                                  if (receivedMessageCount < 3) throw new Exception("oh noes!");

                                                  // the third time, we continue
                                                  resetEvent.Set();
                                              }))
                .Start();

            senderBus.Routing.Send(ReceiverInputQueueName, "HELLO!");

            if (!resetEvent.WaitOne(TimeSpan.FromSeconds(3)))
            {
                Assert.Fail("Did not receive message three times within timeout");
            }
        }

        [Test]
        public void CanSendAndReceiveMessagesLikeExpected()
        {
            var recipientWasCalled = false;
            var manualResetEvent = new ManualResetEvent(false);
            var senderBus = CreateBus(AnotherSenderInputQueueName, new HandlerActivatorForTesting()).Start();

            CreateBus(RecipientQueueName, new HandlerActivatorForTesting()
                                              .Handle<string>(str =>
                                                                  {
                                                                      recipientWasCalled = true;
                                                                      manualResetEvent.Set();
                                                                  }))
                .Start();

            senderBus.Routing.Send(RecipientQueueName, "yo!");
            manualResetEvent.WaitOne(TimeSpan.FromSeconds(5));
            Assert.IsTrue(recipientWasCalled, "The recipient did not receive a call within allotted timeout");
        }

        [Test]
        public void RequestReplyWorks()
        {
            var requestorGotMessageEvent = new ManualResetEvent(false);
            var requestorBus = CreateBus(RequestorQueueName,
                                         new HandlerActivatorForTesting().Handle<string>(
                                             str => requestorGotMessageEvent.Set()));

            var replierHandlerFactory = new HandlerActivatorForTesting();
            var replierBus = CreateBus(ReplierQueueName, replierHandlerFactory);

            replierHandlerFactory.Handle<string>(str => replierBus.Reply("pong!"));

            requestorBus.Start();
            replierBus.Start();
            requestorBus.Routing.Send(ReplierQueueName, "ping?");

            if (!requestorGotMessageEvent.WaitOne(TimeSpan.FromSeconds(3)))
            {
                Assert.Fail("Requestor did not receive a reply within timeout");
            }
        }

        [Test]
        public void PublishSubscribeWorks()
        {
            var publisherBus = CreateBus(PublisherInputQueue, new HandlerActivatorForTesting()).Start();

            var firstSubscriberResetEvent = new AutoResetEvent(false);
            var secondSubscriberResetEvent = new AutoResetEvent(false);

            var firstSubscriberHandlerFactory = new HandlerActivatorForTesting()
                .Handle<string>(s =>
                                    {
                                        if (s == "hello peeps!")
                                        {
                                            firstSubscriberResetEvent.Set();
                                        }
                                    });

            var firstSubscriberBus =
                CreateBus(FirstSubscriberInputQueue, firstSubscriberHandlerFactory).Start();

            firstSubscriberBus.Routing.Subscribe<string>(PublisherInputQueue);

            var secondSubscriberHandlerFactory = new HandlerActivatorForTesting()
                .Handle<string>(s =>
                                    {
                                        if (s == "hello peeps!")
                                        {
                                            secondSubscriberResetEvent.Set();
                                        }
                                    });

            var secondSubscriberBus =
                CreateBus(SecondSubscriberInputQueue, secondSubscriberHandlerFactory).Start();

            secondSubscriberBus.Routing.Subscribe<string>(PublisherInputQueue);

            // allow the publisher to receive the subscriptions....
            Thread.Sleep(500);
            publisherBus.Publish("hello peeps!");

            if (!firstSubscriberResetEvent.WaitOne(TimeSpan.FromSeconds(3)))
            {
                Assert.Fail("First subscriber did not receive the event");
            }

            if (!secondSubscriberResetEvent.WaitOne(TimeSpan.FromSeconds(3)))
            {
                Assert.Fail("Second subscriber did not receive the event");
            }
        }
    }
}