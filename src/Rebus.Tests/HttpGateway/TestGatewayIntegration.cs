using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Transactions;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.HttpGateway;
using Rebus.Logging;
using Rebus.Shared;
using Shouldly;
using System.Linq;

namespace Rebus.Tests.HttpGateway
{
    [TestFixture]
    public class TestGatewayIntegration : RebusBusMsmqIntegrationTestBase
    {
        RebusBus pricedesk;
        RebusBus ordersystem;
        GatewayService outbound;
        GatewayService inbound;
        string pricedeskInputQueue;
        string ordersystemInputQueue;
        HandlerActivatorForTesting orderSystemHandlerActivator;

        protected override void DoSetUp()
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false) { MinLevel = LogLevel.Warn };

            // this one is in DMZ
            pricedeskInputQueue = "test.pricedesk.input";
            pricedesk = CreateBus(pricedeskInputQueue, new HandlerActivatorForTesting());

            // and this one is inside
            ordersystemInputQueue = "test.ordersystem.input";
            orderSystemHandlerActivator = new HandlerActivatorForTesting();
            ordersystem = CreateBus(ordersystemInputQueue, orderSystemHandlerActivator);

            outboundListenQueue = "test.rebus.dmz.gateway";
            MsmqUtil.PurgeQueue(outboundListenQueue);

            // so we set up a one-way gateway service on each side:
            // - the outbound is on the DMZ side
            outbound = new GatewayService
                {
                    ListenQueue = outboundListenQueue,
                    DestinationUri = "http://localhost:" + TestCategories.AvailableHttpPort,
                };

            // and the inbound is on the network domain side
            inbound = new GatewayService
                {
                    ListenUri = "http://+:" + TestCategories.AvailableHttpPort,
                    DestinationQueue = ordersystemInputQueue
                };

            outbound.Start();
            inbound.Start();

            pricedesk.Start(1);
            ordersystem.Start(1);
        }

        protected override void DoTearDown()
        {
            outbound.Stop();
            inbound.Stop();
        }

        [Test]
        public void PricedeskCanSendOrdersToOrdersystemViaGateway()
        {
            // arrange
            var resetEvent = new ManualResetEvent(false);
            orderSystemHandlerActivator.Handle<PlaceOrderRequest>(req =>
                {
                    if (req.What == "beer" && req.HowMuch == 12)
                    {
                        resetEvent.Set();
                    }
                });
            var timeout = 5.Seconds();

            // act
            pricedesk.Send(new PlaceOrderRequest { What = "beer", HowMuch = 12 });

            // assert
            Assert.That(resetEvent.WaitOne(timeout), Is.True, "Request was not received in order system within timeout of {0}", timeout);
        }

        [Test]
        public void CanSendMultipleMessages()
        {
            // arrange
            var receivedMessageCount = 0;
            var resetEvent = new ManualResetEvent(false);
            orderSystemHandlerActivator.Handle<PlaceOrderRequest>(req =>
                {
                    if (req.What == "beer" && req.HowMuch == 12)
                    {
                        var result = Interlocked.Increment(ref receivedMessageCount);

                        if (result == 100)
                        {
                            resetEvent.Set();
                        }
                    }
                });
            var timeout = 10.Seconds();

            // act
            100.Times(() => pricedesk.Send(new PlaceOrderRequest { What = "beer", HowMuch = 12 }));

            // assert
            Assert.That(resetEvent.WaitOne(timeout), Is.True, "Request was not received in order system within timeout of {0}", timeout);
        }

        [TestCase(10, Ignore = true)]
        [TestCase(12, Ignore = true)]
        [TestCase(13, Ignore = true)]
        [TestCase(1000, Ignore = true)]
        [TestCase(1002, Ignore = true)]
        [TestCase(1003, Ignore = true)]
        public void WhenInboundHasProblemsOutboundJustKeepsOnTrucking(int numberOfMessages)
        {
            // arrange
            var receivedMessageCount = 0;
            var messageTracker = new ConcurrentDictionary<Guid, int>();
            var resetEvent = new ManualResetEvent(false);
            orderSystemHandlerActivator.Handle<PlaceOrderRequest>(req =>
                {
                    if (req.What != "beer" || req.HowMuch != 12) return;

                    OnCommit.Do(() =>
                        {
                            messageTracker.AddOrUpdate(req.MsgId, 1, (id, count) => count + 1);

                            var newValue = Interlocked.Increment(ref receivedMessageCount);
                            if (newValue >= numberOfMessages)
                            {
                                resetEvent.Set();
                            }
                        });
                });
            var timeout = numberOfMessages.Seconds();
            var keepMakingChaos = true;

            var chaosMonkey = new Thread(() =>
                {
                    while (keepMakingChaos)
                    {
                        Thread.Sleep(0.1.Seconds());
                        inbound.Stop();
                        Console.WriteLine("Inbound stopped - {0} messages processed...", receivedMessageCount);
                        Thread.Sleep(0.2.Seconds());
                        inbound.Start();
                        Thread.Sleep(2.2331.Seconds());
                    }
                });

            // act
            chaosMonkey.Start();
            numberOfMessages.Times(() => pricedesk.Send(CreateMessage()));

            // assert
            var resetEventWasSet = resetEvent.WaitOne(timeout + 5.Seconds());
            keepMakingChaos = false;
            chaosMonkey.Join();

            // chill, be more sure to empty the queue completely
            Thread.Sleep(1.Seconds());

            Assert.That(resetEventWasSet, Is.True, "Request was not received in order system within timeout of {0}", timeout);
            receivedMessageCount.ShouldBeGreaterThanOrEqualTo(numberOfMessages);
            Console.WriteLine("Actual number of received messages: {0}", receivedMessageCount);
            if (messageTracker.Any(t => t.Value > 1))
            {
                Console.WriteLine(@"The following IDs were received more than once:
{0}", string.Join(Environment.NewLine, messageTracker.Where(t => t.Value > 1).Select(kvp => "    " + kvp.Key + ": " + kvp.Value)));
            }
            messageTracker.Count.ShouldBe(numberOfMessages);
        }

        static PlaceOrderRequest CreateMessage()
        {
            return new PlaceOrderRequest
                {
                    What = "beer",
                    HowMuch = 12,
                    MsgId = Guid.NewGuid(),
                    ArbitraryString = "this is an arbitrary string # " + (counter++),
                };
        }

        static int counter = 1;
        string outboundListenQueue;

        class OnCommit : IEnlistmentNotification
        {
            public static void Do(Action action)
            {
                Assert.That(Transaction.Current, Is.Not.Null, "You can only enlist a commit action if there's an ongoing ambient tx");
                Transaction.Current.EnlistVolatile(new OnCommit(action), EnlistmentOptions.None);
            }

            readonly Action commitAction;

            public OnCommit(Action commitAction)
            {
                this.commitAction = commitAction;
            }

            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                preparingEnlistment.Prepared();
            }

            public void Commit(Enlistment enlistment)
            {
                commitAction();
                enlistment.Done();
            }

            public void Rollback(Enlistment enlistment)
            {
                enlistment.Done();
            }

            public void InDoubt(Enlistment enlistment)
            {
                enlistment.Done();
            }
        }

        // when an order is placed, this request is sent by pricedesk
        class PlaceOrderRequest
        {
            public string What { get; set; }
            public int HowMuch { get; set; }
            public Guid MsgId { get; set; }
            public string ArbitraryString { get; set; }
        }

        public override string GetEndpointFor(Type messageType)
        {
            if (messageType == typeof(PlaceOrderRequest))
            {
                return outbound.ListenQueue;
            }
            return base.GetEndpointFor(messageType);
        }
    }
}