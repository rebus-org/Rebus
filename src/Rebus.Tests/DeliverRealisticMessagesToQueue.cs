using System;
using System.Threading;
using Messages;
using NUnit.Framework;
using Rebus.Persistence.InMemory;
using Rebus.Tests.Integration;

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

            var a = CreateBus(inputQueueNameA, new HandlerActivatorForTesting()
                .Handle<DoSomething>(s =>
                                         {
                                             throw new MuahahahaException();
                                         }),
                              new InMemorySubscriptionStorage(),
                              new SagaDataPersisterForTesting(),
                              inputQueueNameA + ".error").Start(1);

            var b = CreateBus(inputQueueNameB, new HandlerActivatorForTesting());

            var someInteger = 1;

            10.Times(() => a.Routing.Send(inputQueueNameB, new DoSomething { SomeInteger = someInteger++, WhatToDo = "Hello there!" }));

            10.Times(() => b.Routing.Send(inputQueueNameA, new DoSomething { SomeInteger = someInteger++, WhatToDo = "Hello there!" }));

            Thread.Sleep(1500);
        }
    }

    public class MuahahahaException : ApplicationException
    {
        public MuahahahaException()
            : base("MUAHAHAHAHAHAA")
        {
        }
    }
}

namespace Messages
{
    public class DoSomething
    {
        public string WhatToDo { get; set; }
        public int SomeInteger { get; set; }
    }
}