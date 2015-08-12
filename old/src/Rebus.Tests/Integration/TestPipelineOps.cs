using System.Threading;
using NUnit.Framework;
using Rebus.Shared;
using Shouldly;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(TestCategories.Integration)]
    public class TestPipelineOps : RebusBusMsmqIntegrationTestBase
    {
        const string SenderInputQueueName = "test.integration.pipeline.sender";
        const string ReceiverInputQueueName = "test.integration.pipeline.receiver";
        protected override void DoSetUp()
        {
            MsmqUtil.Delete(SenderInputQueueName);
            MsmqUtil.Delete(ReceiverInputQueueName);
        }

        protected override void DoTearDown()
        {
            MsmqUtil.Delete(SenderInputQueueName);
            MsmqUtil.Delete(ReceiverInputQueueName);
        }

        [Test]
        public void AbortingMessageHandlingReallyAbortsMessageHandling()
        {
            var sender = CreateBus(SenderInputQueueName, new HandlerActivatorForTesting()).Start(1);

            var handlerBeforeAbort = new FirstHandler();
            var handlerAfterAbort = new SecondHandler();
            CreateBus(ReceiverInputQueueName, new HandlerActivatorForTesting()
                                                  .UseHandler(handlerAfterAbort)
                                                  .UseHandler(handlerBeforeAbort)).Start(1);

            pipelineInspector.SetOrder(typeof(FirstHandler), typeof(SecondHandler));

            sender.Routing.Send(ReceiverInputQueueName, "wooooolalalalalaal");

            Thread.Sleep(500);

            handlerBeforeAbort.WasExecuted.ShouldBe(true);
            handlerAfterAbort.WasExecuted.ShouldBe(false);
        }

        class FirstHandler : IHandleMessages<string>
        {
            public bool WasExecuted { get; set; }
            public void Handle(string message)
            {
                WasExecuted = true;
                MessageContext.GetCurrent().Abort();
            }
        }

        class SecondHandler : IHandleMessages<string>
        {
            public bool WasExecuted { get; set; }
            public void Handle(string message)
            {
                WasExecuted = true;
            }
        }
    }
}