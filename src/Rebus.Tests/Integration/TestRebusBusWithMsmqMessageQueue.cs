using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(TestCategories.Integration)]
    public class TestRebusBusWithMsmqMessageQueue : RebusBusMsmqIntegrationTestBase
    {
        [Test]
        public void CanReceiveMessagesInTransaction()
        {
            // arrange
            var senderBus = CreateBus("test.tx.sender", new HandlerActivatorForTesting());

            var resetEvent = new ManualResetEvent(false);
            var receivedMessageCount = 0;

            CreateBus("test.tx.receiver",
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

            senderBus.Send("test.tx.receiver", "HELLO!");

            if (!resetEvent.WaitOne(TimeSpan.FromSeconds(3)))
            {
                Assert.Fail("Did not receive message three times within timeout");
            }
        }

        [Test]
        public void CanSendAndReceiveMessagesLikeExpected()
        {
            const string recipientQueueName = "test.recipient";

            var recipientWasCalled = false;
            var manualResetEvent = new ManualResetEvent(false);
            var senderBus = (RebusBus) CreateBus("test.sender", new HandlerActivatorForTesting()).Start();

            CreateBus(recipientQueueName, new HandlerActivatorForTesting()
                                              .Handle<string>(str =>
                                                                  {
                                                                      recipientWasCalled = true;
                                                                      manualResetEvent.Set();
                                                                  }))
                .Start();

            senderBus.Send(recipientQueueName, "yo!");
            manualResetEvent.WaitOne(TimeSpan.FromSeconds(5));
            Assert.IsTrue(recipientWasCalled, "The recipient did not receive a call within allotted timeout");
        }

        [Test]
        public void RequestReplyWorks()
        {
            var requestorQueueName = "test.requestor";
            var replierQueueName = "test.replier";

            var requestorGotMessageEvent = new ManualResetEvent(false);
            var requestorBus = CreateBus(requestorQueueName,
                                         new HandlerActivatorForTesting().Handle<string>(
                                             str => requestorGotMessageEvent.Set()));

            var replierHandlerFactory = new HandlerActivatorForTesting();
            var replierBus = CreateBus(replierQueueName, replierHandlerFactory);

            replierHandlerFactory.Handle<string>(str => replierBus.Reply("pong!"));

            requestorBus.Start();
            replierBus.Start();
            requestorBus.Send(replierQueueName, "ping?");

            if (!requestorGotMessageEvent.WaitOne(TimeSpan.FromSeconds(3)))
            {
                Assert.Fail("Requestor did not receive a reply within timeout");
            }
        }

        [Test]
        public void PublishSubscribeWorks()
        {
            var publisherInputQueue = "test.publisher";
            var publisherBus = CreateBus(publisherInputQueue, new HandlerActivatorForTesting()).Start();

            var firstSubscriberResetEvent = new AutoResetEvent(false);
            var secondSubscriberResetEvent = new AutoResetEvent(false);

            var firstSubscriberInputQueue = "test.subscriber1";
            var firstSubscriberHandlerFactory = new HandlerActivatorForTesting()
                .Handle<string>(s =>
                                    {
                                        if (s == "hello peeps!")
                                        {
                                            firstSubscriberResetEvent.Set();
                                        }
                                    });

            var firstSubscriberBus =
                (RebusBus) CreateBus(firstSubscriberInputQueue, firstSubscriberHandlerFactory).Start();

            firstSubscriberBus.Subscribe<string>(publisherInputQueue);

            var secondSubscriberInputQueue = "test.subscriber2";
            var secondSubscriberHandlerFactory = new HandlerActivatorForTesting()
                .Handle<string>(s =>
                                    {
                                        if (s == "hello peeps!")
                                        {
                                            secondSubscriberResetEvent.Set();
                                        }
                                    });

            var secondSubscriberBus =
                (RebusBus) CreateBus(secondSubscriberInputQueue, secondSubscriberHandlerFactory).Start();

            secondSubscriberBus.Subscribe<string>(publisherInputQueue);

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