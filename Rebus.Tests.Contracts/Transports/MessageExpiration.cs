using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Tests.Contracts.Transports;

public abstract class MessageExpiration<TTransportFactory> : FixtureBase where TTransportFactory : ITransportFactory, new()
{
    TTransportFactory _factory;
    CancellationToken _cancellationToken;

    protected override void SetUp()
    {
        _factory = new TTransportFactory();
        _cancellationToken = new CancellationTokenSource().Token;
    }

    protected override void TearDown()
    {
        _factory.CleanUp();
    }

    [Test]
    public async Task ReceivesNonExpiredMessage()
    {
        var queueName = TestConfig.GetName("expiration");
        var transport = _factory.Create(queueName);
        var id = Guid.NewGuid().ToString();

        using (var scope = new RebusTransactionScope())
        {
            var headers = new Dictionary<string, string>
            {
                {Headers.MessageId, Guid.NewGuid().ToString() },
                {"recognizzle", id}
            };
            await transport.Send(queueName, MessageWith(headers), scope.TransactionContext);
            await scope.CompleteAsync();
        }

        await Task.Delay(5000);

        using (var scope = new RebusTransactionScope())
        {
            var transportMessage = await transport.Receive(scope.TransactionContext, _cancellationToken);
            await scope.CompleteAsync();

            Assert.That(transportMessage, Is.Not.Null);

            var headers = transportMessage.Headers;

            Assert.That(headers.ContainsKey("recognizzle"));
            Assert.That(headers["recognizzle"], Is.EqualTo(id));
        }
    }

    [Test]
    public async Task DoesNotReceiveExpiredMessage()
    {
        var queueName = TestConfig.GetName("expiration");
        var transport = _factory.Create(queueName);
        var id = Guid.NewGuid().ToString();

        using (var scope = new RebusTransactionScope())
        {
            var headers = new Dictionary<string, string>
            {
                {Headers.MessageId, Guid.NewGuid().ToString() },
                {Headers.SentTime, DateTimeOffset.Now.ToString("o") },
                {"recognizzle", id},
                {Headers.TimeToBeReceived, "00:00:04"} //< expires after 4 seconds!
            };
            await transport.Send(queueName, MessageWith(headers), scope.TransactionContext);
            await scope.CompleteAsync();
        }

        const int millisecondsDelay = 7000;

        var stopwatch = Stopwatch.StartNew();
        await Task.Delay(millisecondsDelay);
        Console.WriteLine($"Delay of {millisecondsDelay} ms actually lasted {stopwatch.ElapsedMilliseconds:0} ms");

        using (var scope = new RebusTransactionScope())
        {
            var transportMessage = await transport.Receive(scope.TransactionContext, _cancellationToken);
            await scope.CompleteAsync();

            Assert.That(transportMessage, Is.Null);
        }
    }

    [Test]
    public async Task ReceivesAlmostExpiredMessage()
    {
        var queueName = TestConfig.GetName("expiration");
        var transport = _factory.Create(queueName);
        var id = Guid.NewGuid().ToString();

        using (var scope = new RebusTransactionScope())
        {
            var headers = new Dictionary<string, string>
            {
                {Headers.MessageId, Guid.NewGuid().ToString() },
                {"recognizzle", id},
                {Headers.TimeToBeReceived, "00:00:20"},
                {Headers.SentTime,DateTimeOffset.UtcNow.ToString("O")}//< expires after 10 seconds!
            };
            await transport.Send(queueName, MessageWith(headers), scope.TransactionContext);
            await scope.CompleteAsync();
        }

        await Task.Delay(3000);

        using (var scope = new RebusTransactionScope())
        {
            var transportMessage = await transport.Receive(scope.TransactionContext, _cancellationToken);
            await scope.CompleteAsync();

            Assert.That(transportMessage, Is.Not.Null);
        }
    }

    static TransportMessage MessageWith(Dictionary<string, string> headers)
    {
        return new TransportMessage(headers, DontCareAboutTheBody());
    }

    static byte[] DontCareAboutTheBody()
    {
        return System.Text.Encoding.UTF8.GetBytes("Dont Care About The Body");
    }
}