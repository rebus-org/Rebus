using System.Threading;
using NUnit.Framework;
using Rebus.Logging;
using Shouldly;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(TestCategories.Integration)]
    public class TestDynamicDispatch : RebusBusMsmqIntegrationTestBase
    {
        protected override void DoSetUp()
        {
            RebusLoggerFactory.Current = new NullLoggerFactory();
            base.DoSetUp();
        }

        [Test, Ignore("This is silly")]
        public void HandlerWithDynamicGetAllCalls()
        {
            const string receiverInputQueueName = "test.dynamic.receiver";
            var sender = CreateBus("test.dynamic.sender", new HandlerActivatorForTesting()).Start(1);

            var dynamicHandler = new DynamicHandler();
            CreateBus(receiverInputQueueName, new HandlerActivatorForTesting()
                                                  .UseHandler(dynamicHandler)).Start(1);

            var justSomeMessage = new JustSomeMessage {Greeting = "Hello world!"};
            sender.Send(receiverInputQueueName, justSomeMessage);

            Thread.Sleep(2000);

            dynamicHandler.GotIt.ShouldBe(true);
        }

        public class JustSomeMessage
        {
            public string Greeting { get; set; }
        }
    }

    public class DynamicHandler : IHandleMessages<object>
    {
        public bool GotIt { get; set; }

        public void Handle(dynamic message)
        {
            if (message.Greeting == "Hello world!")
            {
                GotIt = true;
            }
        }
    }
}