using System;
using System.Messaging;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Shared;
using Rebus.Transports.Msmq;
using Rebus.WebAsync;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestWebCallbacks : FixtureBase, IDetermineMessageOwnership
    {
        const string QueueName = "test.webcallbacks.input1";

        [Test]
        public void ItWorks()
        {
            using (var adapter = new BuiltinContainerAdapter())
            {
                Configure.With(adapter)
                         .Transport(t => t.UseMsmq(QueueName, "error"))
                         .MessageOwnership(o => o.Use(this))
                         .AllowWebCallbacks()
                         .CreateBus()
                         .Start();

                var bus = adapter.Bus;
                adapter.Handle<string>(s => bus.Reply("Thank you sir!"));

                var resetEvent = new ManualResetEvent(false);

                adapter.Bus.Send("hello there!", (string reply) =>
                    {
                        Console.WriteLine("Got reply in callback: {0} - YAY!", reply);
                        resetEvent.Set();
                    });

                if (!resetEvent.WaitOne(2.Seconds()))
                {
                    Assert.Fail("Did not receive reply withing 2 seconds of waiting!");
                }
            }
        }

        [Test]
        public void HandlesTypeMismatchGracefully()
        {
            using (var adapter = new BuiltinContainerAdapter())
            {
                Configure.With(adapter)
                         .Transport(t => t.UseMsmq(QueueName, "error"))
                         .MessageOwnership(o => o.Use(this))
                         .AllowWebCallbacks()
                         .CreateBus()
                         .Start();

                var bus = adapter.Bus;
                adapter.Handle<string>(s => bus.Reply("Thank you sir!"));

                var resetEvent = new ManualResetEvent(false);

                adapter.Bus.Send("hello there!", (DateTime reply) =>
                    {
                        resetEvent.Set();
                    });

                if (!resetEvent.WaitOne(2.Seconds()))
                {
                    Assert.Fail("Did not receive reply withing 2 seconds of waiting!");
                }
            }
        }

        protected override void DoTearDown()
        {
            MessageQueue.Delete(MsmqUtil.GetPath(QueueName));
        }

        public string GetEndpointFor(Type messageType)
        {
            if (messageType == typeof (string))
                return QueueName;

            throw new ArgumentException(string.Format("Don't know where to send {0}", messageType));
        }
    }
}