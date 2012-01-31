using System;
using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(TestCategories.Integration)]
    public class TestDynamicDispatch : RebusBusMsmqIntegrationTestBase
    {
        [Test, Ignore("not sure if this one should pass... ?")]
        public void HandlerWithDynamicGetAllCalls()
        {
            const string receiverInputQueueName = "test.dynamic.receiver";
            var sender = CreateBus("test.dynamic.sender", new HandlerActivatorForTesting()).Start(1);

            var dynamicHandler = new DynamicHandler();
            CreateBus(receiverInputQueueName, new HandlerActivatorForTesting()
                                                  .UseHandler(dynamicHandler)).Start(1);

            sender.Send(receiverInputQueueName, new JustSomeMessage{Greeting = "Hello world!"});

            Thread.Sleep(500);

            dynamicHandler.GotIt.ShouldBe(true);
        }

        class JustSomeMessage
        {
            public string Greeting { get; set; }
        }
    }

    public class DynamicHandler : IHandleMessages<object>
    {
        public bool GotIt { get; set; }

        public void Handle(dynamic message)
        {
            Console.WriteLine(message.GetType());

            Console.WriteLine(string.Join(Environment.NewLine, message.GetType().GetType()));

            if (message.Greeting == "Hello world!")
            {
                GotIt = true;
            }
        }
    }
}