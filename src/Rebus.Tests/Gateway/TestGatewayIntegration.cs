using System;
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

        protected override void DoSetUp()
        {
            // this one is in DMZ
            pricedeskInputQueue = "test.pricedesk.input";
            pricedesk = CreateBus(pricedeskInputQueue, new HandlerActivatorForTesting());
            
            // and this one is inside
            ordersystemInputQueue = "test.ordersystem.input";
            ordersystem = CreateBus(ordersystemInputQueue, new HandlerActivatorForTesting());

            // so we set up a gateway on each side:
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
            

            // act

            // assert

        }

        // when an order is placed, this request is sent by pricedesk
        class PlaceOrderRequest
        {
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