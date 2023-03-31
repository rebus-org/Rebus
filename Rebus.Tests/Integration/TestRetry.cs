using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Retry.Simple;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;

#pragma warning disable 1998

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestRetry : FixtureBase
{
    static readonly string InputQueueName = TestConfig.GetName($"test.rebus2.retries.input@{GetMachineName()}");
    static readonly string ErrorQueueName = TestConfig.GetName("rebus2.error");

    BuiltinHandlerActivator _handlerActivator;
    IBus _bus;
    InMemNetwork _network;

    void InitializeBus(int numberOfRetries)
    {
        _network = new InMemNetwork();

        _handlerActivator = new BuiltinHandlerActivator();

        _bus = Configure.With(_handlerActivator)
            .Logging(l => l.Console(minLevel: LogLevel.Warn))
            .Transport(t => t.UseInMemoryTransport(_network, InputQueueName))
            .Routing(r => r.TypeBased().Map<string>(InputQueueName))
            .Options(o => o.RetryStrategy(maxDeliveryAttempts: numberOfRetries, errorQueueName: ErrorQueueName))
            .Start();

        Using(_bus);
    }

    [Test]
    public async Task ItWorks()
    {
        const int numberOfRetries = 5;

        InitializeBus(numberOfRetries);

        var attemptedDeliveries = 0;

        _handlerActivator.AddHandlerWithBusTemporarilyStopped<string>(async _ =>
        {
            Interlocked.Increment(ref attemptedDeliveries);
            throw new RebusApplicationException("omgwtf!");
        });

        await _bus.Send("hej");

        var failedMessage = await _network.WaitForNextMessageFrom(ErrorQueueName);

        Assert.That(attemptedDeliveries, Is.EqualTo(numberOfRetries));
        Assert.That(failedMessage.Headers.GetValue(Headers.ErrorDetails), Contains.Substring("5 unhandled exceptions"));
        Assert.That(failedMessage.Headers.GetValue(Headers.SourceQueue), Is.EqualTo(InputQueueName));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(5)]
    public async Task CanConfigureNumberOfRetries(int numberOfRetries)
    {
        InitializeBus(numberOfRetries);

        var attemptedDeliveries = 0;

        _handlerActivator.AddHandlerWithBusTemporarilyStopped<string>(async _ =>
        {
            Interlocked.Increment(ref attemptedDeliveries);
            throw new RebusApplicationException("omgwtf!");
        });

        await _bus.Send("hej");

        await _network.WaitForNextMessageFrom(ErrorQueueName);

        var expectedNumberOfAttemptedDeliveries = numberOfRetries;

        Assert.That(attemptedDeliveries, Is.EqualTo(expectedNumberOfAttemptedDeliveries));
    }

    static string GetMachineName() => Environment.MachineName;
}