using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(TestCategories.Integration)]
    public class TestPipelineOps : RebusBusMsmqIntegrationTestBase
    {
        [Test]
        public void AbortingMessageHandlingReallyAbortsMessageHandling()
        {
            const string senderInputQueueName = "test.integration.pipeline.sender";
            const string receiverInputQueueName = "test.integration.pipeline.receiver";
            
            var sender = CreateBus(senderInputQueueName, new HandlerActivatorForTesting()).Start(1);

            var handlerBeforeAbort = new FirstHandler();
            var handlerAfterAbort = new SecondHandler();
            CreateBus(receiverInputQueueName, new HandlerActivatorForTesting()
                                                  .UseHandler(handlerAfterAbort)
                                                  .UseHandler(handlerBeforeAbort)).Start(1);

            pipelineInspector.SetOrder(typeof(FirstHandler), typeof(SecondHandler));

            sender.Send(receiverInputQueueName, "wooooolalalalalaal");

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