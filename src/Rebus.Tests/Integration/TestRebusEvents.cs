using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(TestCategories.Integration)]
    public class TestRebusEvents : RebusBusMsmqIntegrationTestBase
    {
        [Test]
        public void RebusRaisesEventsInAllTheRightPlaces()
        {
            var events = new List<string>();
            var receiverInputQueueName = "events.receiver";

            var receiverHandlerActivator = new HandlerActivatorForTesting()
                .Handle<string>(str =>
                                    {
                                        events.Add("Handling message: " + str);
                                        
                                        if (str == "throw")
                                        {
                                            throw new ApplicationException("w00t!");
                                        }
                                    });

            var receiver = CreateBus(receiverInputQueueName, receiverHandlerActivator).Start(1);
            receiver.BeforeMessage += m => events.Add("Before message");
            receiver.AfterMessage += (e, m) => events.Add("After message: " + e);
            receiver.PoisonMessage += m => events.Add("Poison!");
            
            var sender = CreateBus("events.sender", new HandlerActivatorForTesting()).Start(1);
            sender.Send(receiverInputQueueName, "test");
            sender.Send(receiverInputQueueName, "throw");

            Thread.Sleep(500);

            events.ForEach(Console.WriteLine);
            var eventsPerOrdinaryMessage = 3;
            var eventsToMovePoisonMessage = 3;

            events.Count.ShouldBe(eventsPerOrdinaryMessage
                                  + 5 * eventsPerOrdinaryMessage
                                  + eventsToMovePoisonMessage);

            events[0].ShouldBe("Before message");
            events[1].ShouldBe("Handling message: test");
            events[2].ShouldBe("After message: ");

            events[eventsPerOrdinaryMessage].ShouldBe("Before message");
            events[6].ShouldBe("Before message");
            events[9].ShouldBe("Before message");
            events[12].ShouldBe("Before message");
            events[15].ShouldBe("Before message");

            events[4].ShouldBe("Handling message: throw");
            events[7].ShouldBe("Handling message: throw");
            events[10].ShouldBe("Handling message: throw");
            events[13].ShouldBe("Handling message: throw");
            events[16].ShouldBe("Handling message: throw");

            events[5].ShouldStartWith("After message: ");
            events[5].ShouldContain("System.ApplicationException: w00t!");
            events[8].ShouldStartWith("After message: ");
            events[8].ShouldContain("System.ApplicationException: w00t!");
            events[11].ShouldStartWith("After message: ");
            events[11].ShouldContain("System.ApplicationException: w00t!");
            events[14].ShouldStartWith("After message: ");
            events[14].ShouldContain("System.ApplicationException: w00t!");
            events[17].ShouldStartWith("After message: ");
            events[17].ShouldContain("System.ApplicationException: w00t!");

            events[18].ShouldBe("Before message");
            events[19].ShouldBe("Poison!");
            events[20].ShouldBe("After message: ");
        }
    }
}