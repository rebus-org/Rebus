using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Routing.TransportMessages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Routing
{
    [TestFixture]
    public class TestTransportMessageForwarding : FixtureBase
    {
        [Test]
        public async Task CanForwardToMultipleRecipients()
        {
            var network = new InMemNetwork();
            var activator = new BuiltinHandlerActivator();

            Using(activator);

            var recipients = new[] { "recipient-A", "recipient-B" }.ToList();

            recipients.ForEach(network.CreateQueue);

            Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(network, "forwarder"))
                .Routing(t =>
                {
                    t.AddTransportMessageForwarder(async transportMessage => ForwardAction.ForwardTo(recipients));
                })
                .Start();

            await activator.Bus.SendLocal("HEJ MED DIG!!!");

            var transportMessages = await Task.WhenAll(recipients.Select(async queue =>
            {
                var message = await network.WaitForNextMessageFrom(queue);

                return message;
            }));

            Assert.That(transportMessages.Length, Is.EqualTo(2));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task CanIgnoreMessageCompletely(bool ignoreTheMessage)
        {
            var network = new InMemNetwork();
            var activator = new BuiltinHandlerActivator();

            Using(activator);

            var gotTheMessage = new ManualResetEvent(false);

            activator.Handle<string>(async str =>
            {
                gotTheMessage.Set();
            });

            var recipients = new[] { "recipient-A", "recipient-B" }.ToList();

            recipients.ForEach(network.CreateQueue);

            Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(network, "forwarder"))
                .Routing(t =>
                {
                    if (ignoreTheMessage)
                    {
                        t.AddTransportMessageForwarder(async transportMessage => ForwardAction.Ignore());
                    }
                })
                .Start();

            await activator.Bus.SendLocal("HEJ MED DIG!!!");

            if (ignoreTheMessage)
            {
                Assert.That(gotTheMessage.WaitOne(TimeSpan.FromSeconds(0.5)), Is.False);
            }
            else
            {
                Assert.That(gotTheMessage.WaitOne(TimeSpan.FromSeconds(0.5)), Is.True);
            }
        }

        [Test]
        public async Task CanRetryForever()
        {
            const string recipientQueueName = "recipient";
            var network = new InMemNetwork();
            var activator = new BuiltinHandlerActivator();

            Using(activator);

            network.CreateQueue(recipientQueueName);

            var bus = GetFailingBus(activator, network, recipientQueueName, ErrorBehavior.RetryForever);

            await bus.SendLocal("HEJ MED DIG!!!");

            var message = await network.WaitForNextMessageFrom(recipientQueueName);

            Assert.That(Encoding.UTF8.GetString(message.Body), Is.EqualTo(@"""HEJ MED DIG!!!"""));
        }

        [Test]
        public async Task CanFailFastAndForwardToErrorQueue()
        {
            const string recipientQueueName = "recipient";
            var network = new InMemNetwork();
            var activator = new BuiltinHandlerActivator();

            Using(activator);

            network.CreateQueue(recipientQueueName);

            var bus = GetFailingBus(activator, network, recipientQueueName, ErrorBehavior.ForwardToErrorQueue);

            await bus.SendLocal("HEJ MED DIG!!!");

            var message = await network.WaitForNextMessageFrom("error");

            Assert.That(Encoding.UTF8.GetString(message.Body), Is.EqualTo(@"""HEJ MED DIG!!!"""));
            Assert.That(message.Headers.GetValueOrNull(Headers.SourceQueue), Is.EqualTo("forwarder"));
            Assert.That(message.Headers.GetValueOrNull(Headers.ErrorDetails), Does.Contain("fake an error"));
        }

        static IBus GetFailingBus(BuiltinHandlerActivator activator, InMemNetwork network, string recipientQueueName, ErrorBehavior errorBehavior)
        {
            var deliveryAttempts = 0;

            var bus = Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(network, "forwarder"))
                .Routing(t =>
                {
                    t.AddTransportMessageForwarder(async transportMessage =>
                    {
                        deliveryAttempts++;

                        if (deliveryAttempts < 10)
                        {
                            throw new RebusApplicationException("fake an error");
                        }

                        return ForwardAction.ForwardTo(recipientQueueName);
                    }, errorBehavior);
                })
                .Options(o =>
                {
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(1);
                })
                .Start();
            return bus;
        }
    }
}