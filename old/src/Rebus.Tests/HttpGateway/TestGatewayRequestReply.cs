using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.HttpGateway;
using Rebus.Logging;
using Rebus.Shared;

namespace Rebus.Tests.HttpGateway
{
    [TestFixture, Ignore("logic not implemented yet")]
    public class TestGatewayRequestReply : RebusBusMsmqIntegrationTestBase
    {
        string priceDeskInputQueue;
        RebusBus priceDesk;
        string orderSystemInputQueue;
        HandlerActivatorForTesting orderSystemHandlerActivator;
        RebusBus orderSystem;
        string priceDeskGatewayInputQueue;
        GatewayService priceDeskGatewayService;
        GatewayService orderSystemGatewayService;
        HandlerActivatorForTesting priceDeskHandlerActivator;
        string orderSystemGatewayInputQueue;

        protected override void DoSetUp()
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false) { MinLevel = LogLevel.Info };

            // this one is in DMZ
            priceDeskInputQueue = "test.pricedesk.input";
            priceDeskHandlerActivator = new HandlerActivatorForTesting();
            priceDesk = CreateBus(priceDeskInputQueue, priceDeskHandlerActivator);

            // and this one is inside
            orderSystemInputQueue = "test.ordersystem.input";
            orderSystemHandlerActivator = new HandlerActivatorForTesting();
            orderSystem = CreateBus(orderSystemInputQueue, orderSystemHandlerActivator);

            priceDeskGatewayInputQueue = "test.rebus.pricedesk.gateway";
            MsmqUtil.PurgeQueue(priceDeskGatewayInputQueue);

            orderSystemGatewayInputQueue = "test.rebus.ordersystem.gateway";
            MsmqUtil.PurgeQueue(orderSystemGatewayInputQueue);

            // so we set up a one-way gateway service on each side:
            // - the outbound is on the DMZ side
            priceDeskGatewayService = new GatewayService
                {
                    ListenQueue = priceDeskGatewayInputQueue,
                    DestinationUri = "http://localhost:" + TestCategories.AvailableHttpPort,
                };

            // and the inbound is on the network domain side
            orderSystemGatewayService = new GatewayService
                {
                    ListenUri = "http://+:" + TestCategories.AvailableHttpPort,
                    DestinationQueue = orderSystemInputQueue,
                };

            priceDeskGatewayService.Start();
            orderSystemGatewayService.Start();

            priceDesk.Start(1);
            orderSystem.Start(1);
        }

        [Test]
        public void CanRequestReplyViaGateway()
        {
            // arrange
            var resetEvent = new ManualResetEvent(false);
            orderSystemHandlerActivator.Handle<JustSomeRequest>(req =>
                {
                    Console.WriteLine("Got request for {0}", req.ForWhat);
                    orderSystem.Reply(new JustSomeReply { What = "you got " + req.ForWhat });
                });
            priceDeskHandlerActivator.Handle<JustSomeReply>(rep =>
                {
                    Console.WriteLine("Got reply with {0}", rep.What);
                    resetEvent.Set();
                });

            // act
            priceDesk.Send(new JustSomeRequest { ForWhat = "beers!!!11 what else????" });

            // assert
            var timeout = 5.Seconds();
            Assert.That(resetEvent.WaitOne(timeout), Is.True, "Oh noes, reset event wasn't set within timeout of {0}", timeout);
        }

        class JustSomeRequest
        {
            public string ForWhat { get; set; }
        }

        class JustSomeReply
        {
            public string What { get; set; }
        }

        public override string GetEndpointFor(Type messageType)
        {
            if (messageType == typeof(JustSomeRequest))
            {
                return orderSystemInputQueue + "->" + priceDeskGatewayInputQueue;
            }

            return base.GetEndpointFor(messageType);
        }
    }
}