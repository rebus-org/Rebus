using System;
using System.Messaging;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Serialization.Json;
using Rebus.Shared;
using Rebus.Transports.Msmq;
using Shouldly;
using Rebus.Logging;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(TestCategories.Integration)]
    public class TestCorrelationIdHeader : FixtureBase, IDetermineMessageOwnership, IStoreSubscriptions
    {
        const string InputQueueName = "test.correlationId.flow.input";

        BuiltinContainerAdapter adapter;
        IBus bus;
        ManualResetEvent resetEvent;
        string correlationId;

        protected override void DoSetUp()
        {
            adapter = new BuiltinContainerAdapter();

            bus = Configure.With(adapter)
                           .Logging(l => l.ColoredConsole(LogLevel.Warn))
                           .Behavior(b => b.SetMaxRetriesFor<ApplicationException>(0))
                           .Transport(t => t.UseMsmq(InputQueueName, "error"))
                           .Subscriptions(s => s.Use(this))
                           .MessageOwnership(m => m.Use(this))
                           .CreateBus()
                           .Start();

            resetEvent = new ManualResetEvent(false);

            correlationId = "has not been set by anything yet!";
        }

        protected override void DoTearDown()
        {
            adapter.Dispose();

            MessageQueue.Delete(MsmqUtil.GetPath(InputQueueName));
        }

        [Test]
        public void WorksWhenMovingToErrorQueue()
        {
            using (var messageQueue = new MsmqMessageQueue("error"))
            {
                // make sure error queue is empty
                messageQueue.PurgeInputQueue();

                adapter.Handle<Request>(req =>
                {
                    throw new ApplicationException("oh crap");
                });

                var request = new Request();
                bus.AttachHeader(request, Headers.CorrelationId, "super-unique!!!!111");
                bus.SendLocal(request);

                // let it fail
                Thread.Sleep(2.Seconds());

                var receivedTransportMessage = messageQueue.ReceiveMessage(new NoTransaction());
                receivedTransportMessage.ShouldNotBe(null);

                var serializer = new JsonMessageSerializer();
                var message = serializer.Deserialize(receivedTransportMessage);

                message.Headers.ShouldContainKeyAndValue(Headers.CorrelationId, "super-unique!!!!111");
            }
        }

        [Test]
        public void AutomaticallyProvidesCorrelationIdWhenOneHasNotBeenProvided()
        {
            adapter.Handle<Request>(req =>
                {
                    SetCorrelationIdIfPossible();
                    SignalResetEvent();
                });

            bus.SendLocal(new Request());
            BlockOnResetEvent(2.Seconds());

            correlationId.ShouldNotContain("has not been set");
        }

        [Test]
        public void WorksWhenReplying()
        {
            adapter.Handle<Request>(req => bus.Reply(new Reply()));

            adapter.Handle<Reply>(rep =>
                {
                    SetCorrelationIdIfPossible();
                    SignalResetEvent();
                });

            var request = new Request();
            bus.AttachHeader(request, Headers.CorrelationId, "super-unique!!!!111");
            bus.SendLocal(request);

            BlockOnResetEvent(2.Seconds());

            correlationId.ShouldBe("super-unique!!!!111");
        }

        [Test]
        public void WorksWhenPublishing()
        {
            adapter.Handle<Request>(req => bus.Publish(new Event()));

            adapter.Handle<Event>(evt =>
                {
                    SetCorrelationIdIfPossible();
                    SignalResetEvent();
                });

            var request = new Request();
            bus.AttachHeader(request, Headers.CorrelationId, "super-unique!!!!111");
            bus.SendLocal(request);

            BlockOnResetEvent(2.Seconds());

            correlationId.ShouldBe("super-unique!!!!111");
        }

        [Test]
        public void WorksWhenSending()
        {
            adapter.Handle<Request>(req => bus.Send(new AnotherRequest()));

            adapter.Handle<AnotherRequest>(anotherReq =>
                {
                    SetCorrelationIdIfPossible();
                    SignalResetEvent();
                });

            var request = new Request();
            bus.AttachHeader(request, Headers.CorrelationId, "super-unique!!!!111");
            bus.SendLocal(request);

            BlockOnResetEvent(2.Seconds());

            correlationId.ShouldBe("super-unique!!!!111");
        }

        [Test]
        public void WorksWhenSendingToSelf()
        {
            adapter.Handle<Request>(req => bus.SendLocal(new AnotherRequest()));

            adapter.Handle<AnotherRequest>(anotherReq =>
                {
                    SetCorrelationIdIfPossible();
                    SignalResetEvent();
                });

            var request = new Request();
            bus.AttachHeader(request, Headers.CorrelationId, "super-unique!!!!111");
            bus.SendLocal(request);

            BlockOnResetEvent(2.Seconds());

            correlationId.ShouldBe("super-unique!!!!111");
        }

        void SignalResetEvent()
        {
            resetEvent.Set();
        }

        void BlockOnResetEvent(TimeSpan timeout)
        {
            if (resetEvent.WaitOne(timeout)) return;

            Assert.Fail("Did not receive reply within timeout of {0}!", timeout);
        }

        void SetCorrelationIdIfPossible()
        {
            var messageContext = MessageContext.GetCurrent();
            if (messageContext.Headers.ContainsKey(Headers.CorrelationId))
            {
                correlationId = messageContext
                    .Headers[Headers.CorrelationId]
                    .ToString();
            }
        }

        class Request { }
        class Reply { }
        class Event { }
        class AnotherRequest { }

        public string GetEndpointFor(Type messageType)
        {
            // OWN ALL THE THINGS!!!!!!11
            return InputQueueName;
        }

        public string[] GetSubscribers(Type eventType)
        {
            // SUBSCRIBE TO ALL THE THINGS!!!!111
            return new[] { InputQueueName };
        }

        public void Store(Type eventType, string subscriberInputQueue)
        {
            throw new NotImplementedException();
        }

        public void Remove(Type eventType, string subscriberInputQueue)
        {
            throw new NotImplementedException();
        }
    }
}