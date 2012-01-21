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
            timeoutService = new TimeoutService();
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
            client.Send(new RequestTimeoutMessage {CorrelationId = "correlator", Timeout = 2.Seconds()});
            var timeoutExpired = false;

            handlerActivator
                .Handle<TimeoutExpiredMessage>(m =>
                                                   {
                                                       if (m.CorrelationId == "correlator")
                                                       {
                                                           timeoutExpired = true;
                                                       }
                                                   });

            Thread.Sleep(2.5.Seconds());

            timeoutExpired.ShouldBe(true);
        }

        public override string GetEndpointFor(System.Type messageType)
        {
            if (messageType == typeof(RequestTimeoutMessage))
            {
                return timeoutService.InputQueue;
            }

            return base.GetEndpointFor(messageType);
        }
    }
}