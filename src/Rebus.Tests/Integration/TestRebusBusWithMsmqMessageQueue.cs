using System;
using System.Threading;
using NUnit.Framework;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(TestCategories.Integration)]
    public class TestRebusBusWithMsmqMessageQueue : RebusBusMsmqIntegrationTestBase
    {
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
            var requestorBus = CreateBus(requestorQueueName, new TestHandlerFactory().Handle<string>(str => requestorGotMessageEvent.Set()));

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
    }
}