using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Rebus.Shared;
using Rebus.Tests.Configuration;
using Shouldly;
using System.Linq;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(TestCategories.Integration)]
    public class TestRebusEvents : RebusBusMsmqIntegrationTestBase
    {
        const string ErrorQueueName = "error";
        const string ReceiverInputQueueName = "test.events.receiver";
        const string SenderInputQueueName = "test.events.sender";

        protected override void DoSetUp()
        {
            MsmqUtil.Delete(SenderInputQueueName);
            MsmqUtil.Delete(ReceiverInputQueueName);
            MsmqUtil.Delete(ErrorQueueName);

            MsmqUtil.EnsureMessageQueueExists(MsmqUtil.GetPath(ErrorQueueName));
        }

        protected override void DoTearDown()
        {
            MsmqUtil.Delete(SenderInputQueueName);
            MsmqUtil.Delete(ReceiverInputQueueName);
            MsmqUtil.Delete(ErrorQueueName);
        }

        [Test]
        public void RebusRaisesEventsAlsoWhenSendingMessages()
        {
            // arrange
            var senderEvents = new List<string>();
            var receiverEvents = new List<string>();
            var receiver = CreateBus(ReceiverInputQueueName, new HandlerActivatorForTesting().Handle<string>(s => {}));
            receiver.Events.BeforeMessage += (b, m) => receiverEvents.Add("received: " + m);
            receiver.Start();

            var sender = CreateBus(SenderInputQueueName, new HandlerActivatorForTesting());
            sender.Events.MessageSent += (b, e, m) => senderEvents.Add("sent: " + m);
            sender.Start();

            // act
            sender.Routing.Send(ReceiverInputQueueName, "whoa!!");
            Thread.Sleep(0.5.Seconds());

            // assert
            senderEvents.Count.ShouldBe(1);
            senderEvents[0].ShouldBe("sent: whoa!!");

            receiverEvents.Count.ShouldBe(1);
            receiverEvents[0].ShouldBe("received: whoa!!");
        }

        [Test]
        public void RebusRaisesEventsInAllTheRightPlacesWhenReceivingMessages()
        {
            var events = new List<string>();

            var receiverHandlerActivator = new HandlerActivatorForTesting()
                .Handle<string>(str =>
                                    {
                                        events.Add("Handling message: " + str);
                                        
                                        if (str == "throw")
                                        {
                                            throw new ApplicationException("w00t!");
                                        }
                                    });

            var receiver = CreateBus(ReceiverInputQueueName, receiverHandlerActivator).Start(1);
            receiver.Events.BeforeTransportMessage += (b, m) => events.Add("Before message");
            receiver.Events.AfterTransportMessage += (b, e, m) => events.Add(string.Format("After message: {0} - has context: {1}", e, MessageContext.HasCurrent));
            receiver.Events.PoisonMessage += (b, m, i) => events.Add(string.Format("Poison! - {0} exceptions caught", i.Exceptions.Length));
            
            var sender = CreateBus(SenderInputQueueName, new HandlerActivatorForTesting()).Start(1);
            sender.Routing.Send(ReceiverInputQueueName, "test");
            sender.Routing.Send(ReceiverInputQueueName, "throw");

            Thread.Sleep(1.Seconds());

            receiver.SetNumberOfWorkers(0);

            Thread.Sleep(1.Seconds());

            Console.WriteLine(@"------------------------------------------------
Events:

{0}

------------------------------------------------", string.Join(Environment.NewLine, events.Select(e =>
                {
                    var str = e.Replace(Environment.NewLine, "////");
                    return str.Length > 80 ? str.Substring(0, 80) + "(...)" : str;
                })));

            var eventsPerOrdinaryMessage = 3;
            var eventsToMovePoisonMessage = 1;

            events.Count.ShouldBe(eventsPerOrdinaryMessage
                                  + 5 * eventsPerOrdinaryMessage
                                  + eventsToMovePoisonMessage);

            events[0].ShouldBe("Before message");
            events[1].ShouldBe("Handling message: test");
            events[2].ShouldBe("After message:  - has context: True");

            events[3].ShouldBe("Before message");
            events[4].ShouldBe("Handling message: throw");
            events[5].ShouldStartWith("After message: ");
            events[5].ShouldContain("System.ApplicationException: w00t!");
            
            events[6].ShouldBe("Before message");
            events[7].ShouldBe("Handling message: throw");
            events[8].ShouldStartWith("After message: ");
            events[8].ShouldContain("System.ApplicationException: w00t!");
            
            events[9].ShouldBe("Before message");
            events[10].ShouldBe("Handling message: throw");
            events[11].ShouldStartWith("After message: ");
            events[11].ShouldContain("System.ApplicationException: w00t!");
            
            events[12].ShouldBe("Before message");
            events[13].ShouldBe("Handling message: throw");
            events[14].ShouldStartWith("After message: ");
            events[14].ShouldContain("System.ApplicationException: w00t!");
            
            events[15].ShouldBe("Before message");
            events[16].ShouldBe("Handling message: throw");
            events[17].ShouldStartWith("After message: ");
            events[17].ShouldContain("System.ApplicationException: w00t!");

            events[18].ShouldBe("Poison! - 5 exceptions caught");
        }

        [Test]
        public void RebusRaisesEventsWhenStartingAndStopping()
        {
            var busEvents = new List<string>();
            var bus = CreateBus(ReceiverInputQueueName, new TestConfigurationApi.TestContainerAdapter());
            bus.Events.BusStarted += x => busEvents.Add("bus started");
            bus.Events.BusStopped += x => busEvents.Add("bus stopped");

            bus.Start();
            busEvents.Last().ShouldBe("bus started");

            bus.Dispose();
            busEvents.Last().ShouldBe("bus stopped");
        }
    }
}