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
using Rebus.Transport.InMem;
using Rebus.Retry.FailFast;
using Rebus.Tests.Extensions;

#pragma warning disable 1998

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestFailFast : FixtureBase
{
    static readonly string InputQueueName = TestConfig.GetName($"test.rebus2.retries.input@{GetMachineName()}");
    static readonly string ErrorQueueName = TestConfig.GetName("rebus2.error");

    BuiltinHandlerActivator _handlerActivator;
    IBus _bus;
    InMemNetwork _network;

    void InitializeBus(int numberOfRetries, IFailFastChecker failFastChecker = null)
    {
        _network = new InMemNetwork();

        _handlerActivator = new BuiltinHandlerActivator();

        _bus = Configure.With(_handlerActivator)
            .Logging(l => l.Console(minLevel: LogLevel.Warn))
            .Transport(t => t.UseInMemoryTransport(_network, InputQueueName))
            .Routing(r => r.TypeBased().Map<string>(InputQueueName))
            .Options(o =>
            {
                o.SetNumberOfWorkers(1);
                o.SetMaxParallelism(1);

                o.SimpleRetryStrategy(maxDeliveryAttempts: numberOfRetries, errorQueueAddress: ErrorQueueName);

                if (failFastChecker != null)
                {
                    o.Register(_ => failFastChecker);
                }
            })
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
            throw new FailFastException("omgwtf!");
        });

        await _bus.Send("hej");

        var failedMessage = await _network.WaitForNextMessageFrom(ErrorQueueName);

        Assert.That(attemptedDeliveries, Is.EqualTo(1));
        Assert.That(failedMessage.Headers.GetValue(Headers.ErrorDetails), Contains.Substring("1 unhandled exceptions"));
        Assert.That(failedMessage.Headers.GetValue(Headers.SourceQueue), Is.EqualTo(InputQueueName));
    }

    [Test]
    public async Task ItUsesSimpleRetryStrategyWhenCustomException()
    {
        const int numberOfRetries = 5;

        InitializeBus(numberOfRetries);

        var attemptedDeliveries = 0;

        _handlerActivator.AddHandlerWithBusTemporarilyStopped<string>(async _ =>
        {
            Interlocked.Increment(ref attemptedDeliveries);
            throw new InvalidOperationException("omgwtf!");
        });

        await _bus.Send("hej");

        var failedMessage = await _network.WaitForNextMessageFrom(ErrorQueueName);

        Assert.That(attemptedDeliveries, Is.EqualTo(numberOfRetries));
        Assert.That(failedMessage.Headers.GetValue(Headers.ErrorDetails), Contains.Substring($"{numberOfRetries} unhandled exceptions"));
        Assert.That(failedMessage.Headers.GetValue(Headers.SourceQueue), Is.EqualTo(InputQueueName));
    }

    [Test]
    public async Task CanConfigureCustomFailFastChecker()
    {
        const int numberOfRetries = 5;

        InitializeBus(numberOfRetries, new CustomFailFastChecker());

        var attemptedDeliveries = 0;

        _handlerActivator.AddHandlerWithBusTemporarilyStopped<string>(async _ =>
        {
            Interlocked.Increment(ref attemptedDeliveries);
            throw new InvalidOperationException("omgwtf!");
        });

        await _bus.Send("hej");

        var failedMessage = await _network.WaitForNextMessageFrom(ErrorQueueName);

        Assert.That(attemptedDeliveries, Is.EqualTo(1));
        Assert.That(failedMessage.Headers.GetValue(Headers.ErrorDetails), Contains.Substring("1 unhandled exceptions"));
        Assert.That(failedMessage.Headers.GetValue(Headers.SourceQueue), Is.EqualTo(InputQueueName));
    }

    class CustomFailFastChecker : IFailFastChecker
    {
        public bool ShouldFailFast(string messageId, Exception exception)
        {
            return exception is InvalidOperationException;
        }
    }

    static string GetMachineName() => Environment.MachineName;
}