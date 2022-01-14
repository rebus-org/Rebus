using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Transport;
using Rebus.Transport.InMem;

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
}