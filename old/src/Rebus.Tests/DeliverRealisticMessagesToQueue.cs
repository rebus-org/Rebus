using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Persistence.InMemory;
using Rebus.Shared;
using Rebus.Tests.Integration;

namespace Rebus.Tests
{
    [TestFixture, Ignore("not a real test - can be used to plant realistic Rebus messages for the Snoop to look at")]
    public class DeliverRealisticMessagesToQueue : RebusBusMsmqIntegrationTestBase
    {
        const string InputQueueNameA = "a.someQueue";
        const string InputQueueNameB = "b.someQueue";

        [Test]
        public void DoIt()
        {
            var a = CreateBus(InputQueueNameA, new HandlerActivatorForTesting()
                .Handle<DoSomething>(s =>
                {
                    throw new MuahahahaException();
                }),
                new InMemorySubscriptionStorage(),
                new SagaDataPersisterForTesting(),
                InputQueueNameA + ".error").Start(1);

            var b = CreateBus(InputQueueNameB, new HandlerActivatorForTesting());

            var someInteger = 1;

            10.Times(() => a.Routing.Send(InputQueueNameB, new DoSomething { SomeInteger = someInteger++, WhatToDo = "Hello there!" }));

            10.Times(() => b.Routing.Send(InputQueueNameA, new DoSomething { SomeInteger = someInteger++, WhatToDo = "Hello there!" }));

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

    public class DoSomething
    {
        public string WhatToDo { get; set; }
        public int SomeInteger { get; set; }
    }
}