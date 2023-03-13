using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
// ReSharper disable AccessToDisposedClosure
#pragma warning disable CS1998

namespace Rebus.Tests.Transport.InMem;

[TestFixture]
[Description("Verifies the most basic thing about in-mem transport's ability to enlist in an ongoing transaction")]
public class TestInMemTransport : FixtureBase
{
    [Test]
    public async Task VeryBasicTransactionThing()
    {
        const string destinationQueueName = "another-queue";

        var network = new InMemNetwork();

        network.CreateQueue(destinationQueueName);

        var transport = new InMemTransport(network, "test-queue");

        transport.Initialize();

        using (var scope = new RebusTransactionScope())
        {
            var headers = new Dictionary<string, string> { { Headers.MessageId, Guid.NewGuid().ToString() } };

            await transport.Send(
                destinationAddress: destinationQueueName,
                message: new TransportMessage(headers, new byte[] { 1, 2, 3 }),
                context: scope.TransactionContext
            );

            Assert.That(network.Count(destinationQueueName), Is.EqualTo(0),
                $"Expected ZERO messages in queue '{destinationQueueName}' at this point, because the scope was not completed");

            await scope.CompleteAsync();
        }

        Assert.That(network.Count(destinationQueueName), Is.EqualTo(1),
            $"Expected 1 message in queue '{destinationQueueName}' at this point, because the scope is completed now");
    }

    [Test]
    public async Task InMemTransportNowWorksAsCentralizedSubsccriptionStorageToo()
    {
        var network = new InMemNetwork();
        var publisher = CreateBus(network, "publisher");

        using var stringReceivedInSub1 = new ManualResetEvent(initialState: false);
        using var stringReceivedInSub2 = new ManualResetEvent(initialState: false);

        var sub1 = CreateBus(network, "subscriber1", activator => activator.Handle<string>(async str => stringReceivedInSub1.Set()));
        await sub1.Subscribe<string>();

        var sub2 = CreateBus(network, "subscriber2", activator => activator.Handle<string>(async str => stringReceivedInSub2.Set()));
        await sub2.Subscribe<string>();

        await publisher.Publish("HEJ MED DIG MIN VEN! 🙂");

        stringReceivedInSub1.WaitOrDie(TimeSpan.FromSeconds(2), 
            errorMessage: "Did not receive the expected System.String event in sub1 within 2 s");

        stringReceivedInSub2.WaitOrDie(TimeSpan.FromSeconds(2), 
            errorMessage: "Did not receive the expected System.String event in sub2 within 2 s");
    }

    IBus CreateBus(InMemNetwork network, string queueName, Action<BuiltinHandlerActivator> handlers = null)
    {
        var activator = Using(new BuiltinHandlerActivator());

        handlers?.Invoke(activator);

        return Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(network, queueName))
            .Start();
    }
}