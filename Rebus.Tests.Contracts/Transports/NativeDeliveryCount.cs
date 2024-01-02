using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Tests.Contracts.Transports;

public abstract class NativeDeliveryCount<TTransportFactory> : FixtureBase where TTransportFactory : ITransportFactory, new()
{
    TTransportFactory _factory;

    protected override void SetUp()
    {
        _factory = new TTransportFactory();
    }

    protected override void TearDown()
    {
        CleanUpDisposables();

        _factory.CleanUp();
    }

    [Test]
    public async Task CanCountDeliveries()
    {
        var randomQueueName = Guid.NewGuid().ToString();

        var transport = _factory.Create(randomQueueName);

        var headers = new Dictionary<string, string> { [Headers.MessageId] = Guid.NewGuid().ToString("n") };
        var transportMessage = new TransportMessage(headers, Encoding.UTF8.GetBytes("HEJ MED DIG MIN VEN"));

        using (var scope = new RebusTransactionScope())
        {
            await transport.Send(randomQueueName, transportMessage, scope.TransactionContext);
            await scope.CompleteAsync();
        }

        var initialCount = await ReceiveAndGetCount();
        var countAfterRollback = await ReceiveAndGetCount();
        var countAfterAnotherRollback = await ReceiveAndGetCount();

        async Task<int> ReceiveAndGetCount()
        {
            using var scope = new RebusTransactionScope();
            var message = await transport.Receive(scope.TransactionContext, CancellationToken.None);
            var count = GetDeliveryCount(message);
            // just dispose the scope here and let the message go back into the queue
            return count;
        }
    }

    static int GetDeliveryCount(TransportMessage message)
    {
        if (!message.Headers.TryGetValue(Headers.DeliveryCount, out var value))
        {
            throw new AssertionException($"Could not find '{Headers.DeliveryCount}' header among these: {string.Join(", ", message.Headers.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
        }

        if (!int.TryParse(value, out var deliveryCount))
        {
            throw new AssertionException($"Could not parse '{value}' into an integer");
        }

        return deliveryCount;
    }
}

