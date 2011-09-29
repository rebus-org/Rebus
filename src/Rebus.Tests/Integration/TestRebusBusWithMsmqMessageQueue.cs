using System;
using System.Threading;
using NUnit.Framework;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(TestCategories.Integration)]
    public class TestRebusBusWithMsmqMessageQueue : RebusBusMsmqIntegrationTestBase
    {
        [Test]
        public void CanReceiveMessagesInTransaction()
        {
            // arrange
            var senderQueueName = PrivateQueueNamed("test.tx.sender");
            var senderBus = CreateBus(senderQueueName, new TestHandlerFactory());

            var resetEvent = new ManualResetEvent(false);
            var receivedMessageCount = 0;
            var receiverQueueName = PrivateQueueNamed("test.tx.receiver");
            CreateBus(receiverQueueName,
                      new TestHandlerFactory()
                          .Handle<string>(str =>
                                              {
                                                  if (str == "HELLO!")
                                                  {
                                                      receivedMessageCount++;

                                                      Console.WriteLine("msg!");

                                                      // throw the first twi times the message is delivered
                                                      if (receivedMessageCount < 3) throw new Exception("oh noes!");

                                                      // the third time, we continue
                                                      resetEvent.Set();
                                                  }
                                              }))
                .Start();

            senderBus.Send(receiverQueueName, "HELLO!");

            if (!resetEvent.WaitOne(TimeSpan.FromSeconds(3)))
            {
                Assert.Fail("Did not receive message three times within timeout");
            }
        }

        [Test]
        public void CanSendAndReceiveMessagesLikeExpected()
        {
            var recipientWasCalled = false;
            var senderQueueName = PrivateQueueNamed("test.sender");
            var recipientQueueName = PrivateQueueNamed("test.recipient");

            var manualResetEvent = new ManualResetEvent(false);

            var senderBus = CreateBus(senderQueueName, new TestHandlerFactory()).Start();

            CreateBus(recipientQueueName, new TestHandlerFactory()
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
            var requestorQueueName = PrivateQueueNamed("test.requestor");
            var replierQueueName = PrivateQueueNamed("test.replier");

            var requestorGotMessageEvent = new ManualResetEvent(false);
            var requestorBus = CreateBus(requestorQueueName,
                                         new TestHandlerFactory().Handle<string>(str => requestorGotMessageEvent.Set()));

            var replierHandlerFactory = new TestHandlerFactory();
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
            var publisherInputQueue = PrivateQueueNamed("test.publisher");
            var publisherBus = CreateBus(publisherInputQueue, new TestHandlerFactory()).Start();

            var firstSubscriberResetEvent = new AutoResetEvent(false);
            var secondSubscriberResetEvent = new AutoResetEvent(false);

            var firstSubscriberInputQueue = PrivateQueueNamed("test.subscriber1");
            var firstSubscriberHandlerFactory = new TestHandlerFactory()
                .Handle<string>(s =>
                                    {
                                        if (s == "hello peeps!")
                                        {
                                            firstSubscriberResetEvent.Set();
                                        }
                                    });
            var firstSubscriberBus = CreateBus(firstSubscriberInputQueue, firstSubscriberHandlerFactory).Start();
            firstSubscriberBus.Subscribe<string>(publisherInputQueue);

            var secondSubscriberInputQueue = PrivateQueueNamed("test.subscriber2");
            var secondSubscriberHandlerFactory = new TestHandlerFactory()
                .Handle<string>(s =>
                                    {
                                        if (s == "hello peeps!")
                                        {
                                            secondSubscriberResetEvent.Set();
                                        }
                                    });
            var secondSubscriberBus = CreateBus(secondSubscriberInputQueue, secondSubscriberHandlerFactory).Start();
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