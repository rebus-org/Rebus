using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Persistence.InMem;
using Rebus.Routing.Exceptions;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleLiteral
#pragma warning disable 1998

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestRetryExceptionCustomization : FixtureBase
{
    const int SecretErrorCode = 340;
    BuiltinHandlerActivator _activator;
    ListLoggerFactory _listLoggerFactory;

    protected override void SetUp()
    {
        _activator = Using(new BuiltinHandlerActivator());
        _listLoggerFactory = new ListLoggerFactory();

        Configure.With(_activator)
            .Logging(l => l.Use(_listLoggerFactory))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "customize exceptions"))
            .Routing(r =>
            {
                r.ForwardOnException<RebusApplicationException>("error", LogLevel.Error);

                r.ForwardOnException<CustomException>("error", LogLevel.Error, e =>
                {
                    Console.WriteLine("Checking {0}", e);
                    return e.ErrorCode == SecretErrorCode;
                });
            })
            .Options(o => o.LogPipeline(verbose: true))
            .Timeouts(t => t.StoreInMemory())
            .Start();
    }

    [Test]
    public async Task OnlyLogsOneSingleLineWhenForwarding()
    {
        _activator.AddHandlerWithBusTemporarilyStopped<ShouldFail>(async msg => throw new RebusApplicationException("oh no!!!!"));

        await _activator.Bus.SendLocal(new ShouldFail());

        await Task.Delay(2000);

        var significantStuff = _listLoggerFactory.Where(l => l.Level >= LogLevel.Warn).ToList();

        Console.WriteLine(string.Join(Environment.NewLine, significantStuff.Select(l => l.Text.Limit(140, singleLine: true))));

        Assert.That(significantStuff.Count, Is.EqualTo(1), @"Only expected one single ERROR level log line with all the action - got this: 

{0}", string.Join(Environment.NewLine, _listLoggerFactory.Select(l => l.Text.Limit(150, singleLine: true))));
    }

    [Test]
    public async Task MakesOnlyOneSingleDeliveryAttempt()
    {
        var deliveryAttempts = 0;

        _activator.AddHandlerWithBusTemporarilyStopped<ShouldFail>(async msg =>
        {
            Interlocked.Increment(ref deliveryAttempts);

            throw new RebusApplicationException("oh noooo!!!!");
        });

        await _activator.Bus.SendLocal(new ShouldFail());

        await Task.Delay(2000);

        Assert.That(deliveryAttempts, Is.EqualTo(1), "Only expected one single delivery attempt because we have disabled retries for ApplicationException");
    }

    [Test]
    public async Task MakesOnlyOneSingleDeliveryAttemptWhenForwardingOnExceptionThatSatisfiesPredicate()
    {
        var deliveryAttempts = 0;

        _activator.AddHandlerWithBusTemporarilyStopped<ShouldFail>(async msg =>
        {
            Interlocked.Increment(ref deliveryAttempts);

            throw new CustomException { ErrorCode = SecretErrorCode };
        });

        await _activator.Bus.SendLocal(new ShouldFail());

        await Task.Delay(2000);

        Assert.That(deliveryAttempts, Is.EqualTo(1), "Only expected one single delivery attempt because we threw a CustomException with ErrorCode = SecretErrorCode");
    }

    [Test]
    public async Task PerformsTheUsualRetriesOnExceptionsThatDoNotSatisfyThePredicate()
    {
        var deliveryAttempts = 0;

        _activator.AddHandlerWithBusTemporarilyStopped<ShouldFail>(async msg =>
        {
            Interlocked.Increment(ref deliveryAttempts);

            throw new CustomException { ErrorCode = SecretErrorCode + 23 };
        });

        await _activator.Bus.SendLocal(new ShouldFail());

        await Task.Delay(2000);

        Assert.That(deliveryAttempts, Is.EqualTo(5), @"Expected the usual retries because we threw a CustomException that did not satisfy the predicate - here's what happened:

{0}", string.Join(Environment.NewLine, _listLoggerFactory.Select(l => l.Text.Limit(150, singleLine: true))));
    }

    class ShouldFail
    {
    }

    class CustomException : Exception
    {
        public int ErrorCode { get; set; }
    }
}