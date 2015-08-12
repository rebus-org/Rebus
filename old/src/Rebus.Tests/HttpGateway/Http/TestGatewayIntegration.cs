using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.HttpGateway;
using Rebus.Logging;
using Rebus.Shared;

namespace Rebus.Tests.HttpGateway.Http
{
    [TestFixture]
    public class TestGatewayIntegration : RebusBusMsmqIntegrationTestBase
    {
        const string PriceDeskInputQueue = "test.pricedesk.input";
        const string OrderSystemInputQueue = "test.ordersystem.input";
        const string GatewayListeningQueue = "test.rebus.dmz.gateway";
        RebusBus pricedesk;
        RebusBus ordersystem;
        GatewayService gatewayInDmz;
        GatewayService gatewayInside;
        HandlerActivatorForTesting orderSystemHandlerActivator;

        protected override void DoSetUp()
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false) { MinLevel = LogLevel.Warn };

            MsmqUtil.Delete(PriceDeskInputQueue);
            MsmqUtil.Delete(OrderSystemInputQueue);
            MsmqUtil.Delete(GatewayListeningQueue);

            // this one is in DMZ
            pricedesk = CreateBus(PriceDeskInputQueue, new HandlerActivatorForTesting());

            // and this one is inside
            orderSystemHandlerActivator = new HandlerActivatorForTesting();
            ordersystem = CreateBus(OrderSystemInputQueue, orderSystemHandlerActivator);

            // so we set up a one-way gateway service on each side:
            gatewayInDmz = new GatewayService
                {
                    ListenQueue = GatewayListeningQueue,
                    DestinationUri = "http://localhost:18080",
                };
            gatewayInside = new GatewayService
                {
                    ListenUri = "http://+:18080",
                    DestinationQueue = OrderSystemInputQueue
                };

            gatewayInDmz.Start();
            gatewayInside.Start();

            pricedesk.Start(1);
            ordersystem.Start(1);
        }

        protected override void DoTearDown()
        {
            gatewayInDmz.Stop();
            gatewayInside.Stop();

            MsmqUtil.Delete(PriceDeskInputQueue);
            MsmqUtil.Delete(OrderSystemInputQueue);
            MsmqUtil.Delete(GatewayListeningQueue);
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
            100.Times(() => pricedesk.Send(new PlaceOrderRequest {What = "beer", HowMuch = 12}));

            // assert
            Assert.That(resetEvent.WaitOne(timeout), Is.True, "Request was not received in order system within timeout of {0}", timeout);
        }

        // when an order is placed, this request is sent by pricedesk
        class PlaceOrderRequest
        {
            public string What { get; set; }
            public int HowMuch { get; set; }
        }

        public override string GetEndpointFor(Type messageType)
        {
            if (messageType == typeof(PlaceOrderRequest))
            {
                return gatewayInDmz.ListenQueue;
            }
            return base.GetEndpointFor(messageType);
        }
    }
}