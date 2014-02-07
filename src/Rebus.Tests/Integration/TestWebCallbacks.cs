using System;
using System.Messaging;
using System.Threading;
using NUnit.Framework;
using Rebus.Async;
using Rebus.Configuration;
using Rebus.Shared;
using Rebus.Transports.Msmq;
using Shouldly;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestWebCallbacks : FixtureBase, IDetermineMessageOwnership
    {
        BuiltinContainerAdapter adapter;
        IBus bus;
        const string QueueName = "test.webcallbacks.input1";

        protected override void DoSetUp()
        {
            DeleteQueueIfExists();

            adapter = new BuiltinContainerAdapter();

            Configure.With(adapter)
                     .Transport(t => t.UseMsmq(QueueName, "error"))
                     .MessageOwnership(o => o.Use(this))
                     .EnableInlineReplyHandlers()
                     .CreateBus()
                     .Start();

            bus = adapter.Bus;
        }

        protected override void DoTearDown()
        {
            adapter.Dispose();

            DeleteQueueIfExists();
        }

        static void DeleteQueueIfExists()
        {
            if (MessageQueue.Exists(MsmqUtil.GetPath(QueueName)))
            {
                MsmqUtil.Delete(QueueName);
            }
        }

        [Test]
        public void BasicThingWorks()
        {
            adapter.Handle<SomeRequest>(req => bus.Reply(new SomeReply { Message = "Thank you sir!" }));

            var resetEvent = new ManualResetEvent(false);

            adapter.Bus.Send(new SomeRequest {Message = "hello there!"},
                             (SomeReply reply) =>
                                 {
                                     Console.WriteLine("Got reply in callback: {0} - YAY!", reply.Message);
                                     resetEvent.Set();
                                 });

            if (!resetEvent.WaitOne(2.Seconds()))
            {
                Assert.Fail("Did not receive reply within 2 seconds of waiting!");
            }
        }

        [Test]
        public void HandlesTimeoutAsWell()
        {
            adapter.Handle<SomeRequest>(req =>
                {
                    // hah! just ignore it
                });

            var resetEvent = new ManualResetEvent(false);

            adapter.Bus.Send(new SomeRequest {Message = "hello there!"},
                             (SomeReply reply) => Assert.Fail("we should not receive any reply!"),
                             TimeSpan.FromSeconds(2),
                             () => resetEvent.Set());

            if (!resetEvent.WaitOne(3.Seconds()))
            {
                Assert.Fail("Did not receive timeout callback within 3 seconds of waiting!");
            }
        }

        [Test]
        public void DoesNotReceiveMultipleCallbacks()
        {
            adapter.Handle<SomeRequest>(s => bus.Reply(new SomeReply { Message = "Thank you sir!" }));

            var callbackCounter = 0;

            var resetEvent = new ManualResetEvent(false);

            adapter.Bus.Send(new SomeRequest {Message = "hello there!"},
                             (SomeReply reply) =>
                                 {
                                     Console.WriteLine("Got reply in callback: {0} - YAY!", reply.Message);
                                     Interlocked.Increment(ref callbackCounter);
                                     resetEvent.Set();
                                 });

            if (!resetEvent.WaitOne(2.Seconds()))
            {
                Assert.Fail("Did not receive reply within 2 seconds of waiting!");
            }

            // allow for possible additional callbacks to happen...
            Thread.Sleep(1.Seconds());

            callbackCounter.ShouldBe(1);
        }

        [Test]
        public void HandlesTypeMismatchGracefully()
        {
            adapter.Handle<SomeRequest>(req =>
                {
                    Console.WriteLine("Got request: {0}. Will send reply", req.Message);
                    bus.Reply(new SomeReply { Message = "Thank you sir!" });
                });

            var resetEvent = new ManualResetEvent(false);

            adapter.Bus.Send(new SomeRequest {Message = "hello there!"},
                             (DateTime reply) =>
                                 {
                                     Console.WriteLine("Whoa! Got reply: {0}", reply);
                                     resetEvent.Set();
                                 });

            if (resetEvent.WaitOne(2.Seconds()))
            {
                Assert.Fail("Received reply within 2 seconds of waiting in spite of a type mismatch");
            }
        }

        class SomeRequest
        {
            public string Message { get; set; }
        }

        class SomeReply
        {
            public string Message { get; set; }
        }

        public string GetEndpointFor(Type messageType)
        {
            if (messageType == typeof(SomeRequest))
                return QueueName;

            throw new ArgumentException(string.Format("Don't know where to send {0}", messageType));
        }
    }
}