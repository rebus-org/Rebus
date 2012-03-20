using NUnit.Framework;

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

            25.Times(() => b.Send(inputQueueNameA, new DoSomething {SomeInteger = 23, WhatToDo = "Hello there!"}));
        }

        public class DoSomething
        {
            public string WhatToDo { get; set; }
            public int SomeInteger { get; set; }
        }
    }
}