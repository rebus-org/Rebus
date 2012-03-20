using NUnit.Framework;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Shared;

namespace Rebus.Tests
{
    [TestFixture, Ignore("not a real test - can be used to plant realistic Rebus messages for the Snoop to look at")]
    public class DeliverRealisticMessagesToQueue : RebusBusMsmqIntegrationTestBase
    {
        [Test]
        public void DoIt()
        {
            const string inputQueueNameA = "a.someQueue";
            const string inputQueueNameB = "b.someQueue";

            var a = CreateBus(inputQueueNameA, new HandlerActivatorForTesting());   
            var b = CreateBus(inputQueueNameB, new HandlerActivatorForTesting());

            var attach = false;
            25.Times(() =>
                         {
                             var message = new DoSomething {SomeInteger = 23, WhatToDo = "Hello there!"};
                             if (attach)
                             {
                                 message.AttachHeader(Headers.SourceQueue, inputQueueNameB);
                                 attach = false;
                             }
                             else
                             {
                                 attach = true;
                             }
                             b.Send(inputQueueNameA, message);
                         });
        }

        public class DoSomething
        {
            public string WhatToDo { get; set; }
            public int SomeInteger { get; set; }
        }
    }
}