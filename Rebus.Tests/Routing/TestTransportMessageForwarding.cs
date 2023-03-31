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
using Rebus.Retry.Simple;
using Rebus.Routing.TransportMessages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Routing;

[TestFixture]
public class TestTransportMessageForwarding : FixtureBase
{
    [Test]
    [Description("Test forwarding scenario where error queue is unavailable for some reason")]
    public async Task ErrorInErrorQueue()
    {
        var network = new InMemNetwork();
        var activator = Using(new BuiltinHandlerActivator());

        // count send operations to error queue with this one
        var sendToErrorQueueAttempts = 0;

        // hook into send operations
        void SendCallback(string queue)
        {
            if (queue != "error") return; //< don't care about other queues

            sendToErrorQueueAttempts++;

            Console.WriteLine($"{nameof(SendCallback)} invoked - sendToErrorQueueAttempts: {sendToErrorQueueAttempts}");

            // fail on the very first attempt
            if (sendToErrorQueueAttempts == 1)
            {
                throw new ApplicationException("FAILURE ON FIRST ATTEMPT!");
            }
        }

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(network, "whatever"))
            .Routing(r => r.AddTransportMessageForwarder(async msg => throw new AbandonedMutexException("oh no it's been abandoned")))
            .Options(o =>
            {
                o.Decorate<ITransport>(c => new FailureTransportDecorator(SendCallback, c.Get<ITransport>()));
                o.SetRetryStrategy(errorQueueErrorCooldownTimeSeconds: 1);
            })
            .Start();

        await activator.Bus.SendLocal("HEJ MED DIG MIN VEN");

        var failedMessage = await network.WaitForNextMessageFrom("error", timeoutSeconds: 5);
        var receivedString = Encoding.UTF8.GetString(failedMessage.Body);

        Assert.That(receivedString, Is.EqualTo("\"HEJ MED DIG MIN VEN\""));
    }

    class FailureTransportDecorator : ITransport
    {
        readonly Action<string> _sendCallback;
        readonly ITransport _transport;

        public FailureTransportDecorator(Action<string> sendCallback, ITransport transport)
        {
            _sendCallback = sendCallback;
            _transport = transport;
        }

        public void CreateQueue(string address) => _transport.CreateQueue(address);

        public Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            _sendCallback(destinationAddress);
            return _transport.Send(destinationAddress, message, context);
        }

        public Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken) => _transport.Receive(context, cancellationToken);

        public string Address => _transport.Address;
    }

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

        using (var activator = new BuiltinHandlerActivator())
        {
            var bus = GetFailingBus(activator, network, recipientQueueName, ErrorBehavior.RetryForever);

            await bus.SendLocal("HEJ MED DIG!!!");

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

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

        var bus = GetFailingBus(activator, network, recipientQueueName, ErrorBehavior.Normal);

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
                t.AddTransportMessageForwarder(async _ =>
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