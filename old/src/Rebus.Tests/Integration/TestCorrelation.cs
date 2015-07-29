using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Shared;
using Rebus.Transports.Msmq;
using Shouldly;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestCorrelation : FixtureBase
    {
        const string QueueName = "test.correlation.input";
        BuiltinContainerAdapter adapter;
        IBus bus;

        protected override void DoSetUp()
        {
            adapter = new BuiltinContainerAdapter();

            Configure.With(adapter)
                     .Transport(t => t.UseMsmq(QueueName, "error"))
                     .CreateBus()
                     .Start();

            bus = adapter.Bus;
        }

        protected override void DoTearDown()
        {
            adapter.Dispose();
            MsmqUtil.Delete(QueueName);
        }

        [Test]
        public void IncludedCorrelationIdIsAutomaticallyTransferredToReplies()
        {
            // arrange
            var resetEvent = new ManualResetEvent(false);
            adapter.Handle<SomeRequest>(req => bus.Reply(new SomeReply()));

            string receivedCorrelationId = null;
            adapter.Handle<SomeReply>(rep =>
                {
                    var currentHeaders = MessageContext
                        .GetCurrent()
                        .Headers;

                    receivedCorrelationId = currentHeaders.ContainsKey(Headers.CorrelationId)
                                                ? currentHeaders[Headers.CorrelationId].ToString()
                                                : "could not find correlation ID in headers";

                    resetEvent.Set();
                });

            var someRequest = new SomeRequest();
            bus.AttachHeader(someRequest, Headers.CorrelationId, "wootadafook!");

            // act
            bus.SendLocal(someRequest);

            if (!resetEvent.WaitOne(2.Seconds()))
            {
                Assert.Fail("Did not receive any reply withing 2 seconds of waiting");
            }

            // assert
            receivedCorrelationId.ShouldBe("wootadafook!");
        }

        class SomeRequest{}
        class SomeReply{}
    }
}