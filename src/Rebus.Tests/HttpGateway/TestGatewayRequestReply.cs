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
        string pricedeskInputQueue;
        RebusBus pricedesk;
        string ordersystemInputQueue;
        HandlerActivatorForTesting orderSystemHandlerActivator;
        RebusBus ordersystem;
        string outboundListenQueue;
        GatewayService outbound;
        GatewayService inbound;
        HandlerActivatorForTesting priceDeskHandlerActivator;

        protected override void DoSetUp()
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false) { MinLevel = LogLevel.Info };

            // this one is in DMZ
            pricedeskInputQueue = "test.pricedesk.input";
            priceDeskHandlerActivator = new HandlerActivatorForTesting();
            pricedesk = CreateBus(pricedeskInputQueue, priceDeskHandlerActivator);

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
                    DestinationUri = "http://localhost:8080",
                };

            // and the inbound is on the network domain side
            inbound = new GatewayService
                {
                    ListenUri = "http://+:8080",
                    DestinationQueue = ordersystemInputQueue,
                };

            outbound.Start();
            inbound.Start();

            pricedesk.Start(1);
            ordersystem.Start(1);
        }

        [Test]
        public void CanRequestReplyViaGateway()
        {
            // arrange
            var resetEvent = new ManualResetEvent(false);
            orderSystemHandlerActivator.Handle<JustSomeRequest>(req =>
                {
                    Console.WriteLine("Got request for {0}", req.ForWhat);
                    ordersystem.Reply(new JustSomeReply {What = "you got " + req.ForWhat});
                });
            priceDeskHandlerActivator.Handle<JustSomeReply>(rep =>
                {
                    Console.WriteLine("Got reply with {0}", rep.What);
                    resetEvent.Set();
                });

            // act
            pricedesk.Send(outboundListenQueue, new JustSomeRequest {ForWhat = "beers!!!11 what else????"});

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
    }
}