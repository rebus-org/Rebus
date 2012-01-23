using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Timeout;
using Shouldly;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestTimeoutService : RebusBusMsmqIntegrationTestBase
    {
        TimeoutService timeoutService;
        IBus client;
        HandlerActivatorForTesting handlerActivator;

        protected override void DoSetUp()
        {
            timeoutService = new TimeoutService(new InMemoryTimeoutStorage());
            timeoutService.Start();

            handlerActivator = new HandlerActivatorForTesting();
            client = CreateBus("test.rebus.timeout.client", handlerActivator).Start(1);
        }

        protected override void DoTearDown()
        {
            timeoutService.Stop();
        }

        [Test]
        public void WillCallBackAfterTimeHasElapsed()
        {
            var justSomeCorrelationId = Guid.NewGuid().ToString();
            client.Send(new TimeoutRequest {CorrelationId = justSomeCorrelationId, Timeout = 2.Seconds()});
            var timeoutExpired = false;

            handlerActivator
                .Handle<TimeoutReply>(m =>
                                          {
                                              if (m.CorrelationId == justSomeCorrelationId)
                                              {
                                                  timeoutExpired = true;
                                              }
                                          });

            Thread.Sleep(2.5.Seconds());

            timeoutExpired.ShouldBe(true);
        }

        public override string GetEndpointFor(Type messageType)
        {
            if (messageType == typeof(TimeoutRequest))
            {
                return timeoutService.InputQueue;
            }

            return base.GetEndpointFor(messageType);
        }
    }
}