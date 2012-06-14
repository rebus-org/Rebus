using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Gateway;

namespace Rebus.Tests.Gateway
{
    [TestFixture, Ignore("not relevant for now")]
    public class TestGatewayIntegration : RebusBusMsmqIntegrationTestBase
    {
        RebusBus pricedesk;
        RebusBus ordersystem;
        GatewayService gatewayInDmz;
        GatewayService gatewayInside;
        string pricedeskInputQueue;
        string ordersystemInputQueue;
        HandlerActivatorForTesting orderSystemHandlerActivator;

        protected override void DoSetUp()
        {
            // this one is in DMZ
            pricedeskInputQueue = "test.pricedesk.input";
            pricedesk = CreateBus(pricedeskInputQueue, new HandlerActivatorForTesting());
            
            // and this one is inside
            ordersystemInputQueue = "test.ordersystem.input";
            orderSystemHandlerActivator = new HandlerActivatorForTesting();
            ordersystem = CreateBus(ordersystemInputQueue, orderSystemHandlerActivator);

            // so we set up a one-way gateway service on each side:
            gatewayInDmz = new GatewayService
                {
                    ListenQueue = "test.rebus.dmz.gateway",
                    DestinationUri = "http://localhost:8080",
                };
            gatewayInside = new GatewayService
                {
                    ListenUri = "http://+:8080",
                    DestinationQueue = ordersystemInputQueue
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

            pricedesk.Dispose();
            ordersystem.Dispose();
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
            var timeout = 14.Seconds();

            // act
            pricedesk.Send(new PlaceOrderRequest { What = "beer", HowMuch = 12 });

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