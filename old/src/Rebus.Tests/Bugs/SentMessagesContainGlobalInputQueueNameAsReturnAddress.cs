using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Shared;
using Shouldly;

namespace Rebus.Tests.Bugs
{
    [TestFixture, Description("Verifies that the sender points to a machine name in addition to the queue name.")]
    public class SentMessagesContainGlobalInputQueueNameAsReturnAddress : RebusBusMsmqIntegrationTestBase
    {
        const string RecipientInputQueueName = "test.bug.anotherInputQueue";
        const string SenderInputQueueName = "test.bug.someInputQueue";

        RebusBus sender;
        RebusBus recipient;
        HandlerActivatorForTesting recipientHandlers;

        protected override void DoSetUp()
        {
            sender = CreateBus(SenderInputQueueName, new HandlerActivatorForTesting()).Start(1);
            
            recipientHandlers = new HandlerActivatorForTesting();
            recipient = CreateBus(RecipientInputQueueName, recipientHandlers).Start(1);
        }

        protected override void DoTearDown()
        {
            MsmqUtil.Delete(RecipientInputQueueName);
            MsmqUtil.Delete(SenderInputQueueName);
        }

        [Test]
        public void SendersInputQueueUsesGlobalAddressing()
        {
            // arrange
            var resetEvent = new ManualResetEvent(false);
            var returnAddress = "";
            recipientHandlers.Handle<string>(s =>
                {
                    returnAddress = MessageContext.GetCurrent().ReturnAddress;
                    resetEvent.Set();
                });

            // act
            sender.Routing.Send(RecipientInputQueueName, "yo dawg!!");

            // assert
            Assert.That(resetEvent.WaitOne(1.Seconds()), Is.True, "The message was not received withing timeout of 1 second");
            returnAddress.ShouldBe(SenderInputQueueName + "@" + Environment.MachineName);
        }

        public override string GetEndpointFor(Type messageType)
        {
            if (messageType == typeof(string))
                return RecipientInputQueueName;

            return base.GetEndpointFor(messageType);
        }
    }
}