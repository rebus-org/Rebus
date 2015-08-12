using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Persistence.InMemory;
using Rebus.Timeout;
using Shouldly;

namespace Rebus.Tests.Timeout
{
    [TestFixture]
    public class TestTimeoutService : RebusBusMsmqIntegrationTestBase
    {
        const string TimeoutServiceInputQueueName = "rebus.timeout.test.input";
        const string TimeoutServiceErrorQueueName = "rebus.timeout.test.error";
        TimeoutService timeoutService;
        IBus client;
        HandlerActivatorForTesting handlerActivator;

        protected override void DoSetUp()
        {
            timeoutService = new TimeoutService(new InMemoryTimeoutStorage(), TimeoutServiceInputQueueName, TimeoutServiceErrorQueueName);
            timeoutService.Start();

            handlerActivator = new HandlerActivatorForTesting();
            client = CreateBus("test.rebus.timeout.client", handlerActivator).Start(1);
        }

        protected override void DoTearDown()
        {
            timeoutService.Stop();
        }

        [Test(Description = "Verifies that timeouts are properly marked as processed after their corresponding replies have been sent")]
        public void DoesNotReturnTheSameTimeoutMultipleTimes()
        {
            // arrange
            var receivedCorrelationIds = new List<string>();
            var correlationIdWeCanRecognize = Guid.NewGuid().ToString();
            handlerActivator.Handle<TimeoutReply>(reply => receivedCorrelationIds.Add(reply.CorrelationId));

            // act
            client.Send(new TimeoutRequest
                            {
                                CorrelationId = correlationIdWeCanRecognize,
                                Timeout = 2.Seconds(),
                            });
            Thread.Sleep(5.Seconds());

            // assert
            receivedCorrelationIds.Count.ShouldBe(1);
        }

        [Test]
        public void WillCallBackAfterTimeHasElapsed()
        {
            var justSomeCorrelationId = Guid.NewGuid().ToString();
            var justSomeSubCorrelationId = Guid.NewGuid().ToString();
            client.Send(new TimeoutRequest
                            {
                                CorrelationId = justSomeCorrelationId, 
                                CustomData = justSomeSubCorrelationId,
                                Timeout = 2.Seconds()
                            });
            var timeoutExpired = false;

            handlerActivator
                .Handle<TimeoutReply>(m =>
                                          {
                                              Assert.AreEqual(justSomeCorrelationId, m.CorrelationId, "Correlation ID was wrong");
                                              Assert.AreEqual(justSomeSubCorrelationId, m.CustomData, "Custom data was wrong");

                                              timeoutExpired = true;
                                          });

            Thread.Sleep(2.5.Seconds());

            timeoutExpired.ShouldBe(true);
        }

        [Test]
        public void WillNotCallBackBeforeTimeHasElapsed()
        {
            var timeoutExpired = false;
            client.Send(new TimeoutRequest
                            {
                                Timeout = 2.Seconds()
                            });

            handlerActivator.Handle<TimeoutReply>(m => { timeoutExpired = true; });

            Thread.Sleep(0.5.Seconds());

            timeoutExpired.ShouldBe(false);
        }

        public override string GetEndpointFor(Type messageType)
        {
            if (messageType == typeof(TimeoutRequest))
            {
                return TimeoutServiceInputQueueName;
            }

            return base.GetEndpointFor(messageType);
        }
    }
}