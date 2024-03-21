using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Rebus.Tests.Bugs;

[TestFixture]
public class FailFastStillInvokesSecondLevelRetries : FixtureBase
{
    [Test]
    public async Task ItWorksAsIndicatedByTheNameOfThisTestFixture()
    {
        var events = new ConcurrentQueue<string>();

        using var activator = new BuiltinHandlerActivator();

        activator.Handle<string>(async _ =>
        {
            events.Enqueue("string handled the first time - throwing FailFastException!");
            throw new FailFastException("wooH00");
        });

        activator.Handle<IFailed<string>>(async _ => events.Enqueue("2nd level delivery attempt!"));

        var bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new(), "whatever"))
            .Options(o => o.RetryStrategy(secondLevelRetriesEnabled: true))
            .Start();

        await bus.SendLocal("🙂");

        await events.WaitUntil(e => e.Count >= 2);

        Assert.That(events, Is.EqualTo(new[]
        {
            "string handled the first time - throwing FailFastException!",
            "2nd level delivery attempt!",
        }));

    }

    [Test]
    public async Task SecondLevelRetryAfterFailFastExceptionStillHasTheCaughtExceptions()
    {
        var events = new ConcurrentQueue<string>();

        using var activator = new BuiltinHandlerActivator();

        activator.Handle<string>(async _ => throw new FailFastException("wooH00"));

        activator.Handle<IFailed<string>>(async failed => events.Enqueue($"Tracker had {failed.Exceptions.Count()} exceptions"));

        var bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new(), "whatever"))
            .Options(o => o.RetryStrategy(secondLevelRetriesEnabled: true))
            .Start();

        await bus.SendLocal("🙂");

        await events.WaitUntil(e => e.Count >= 1);

        Assert.That(events, Is.EqualTo(new[]
        {
            "Tracker had 1 exceptions",
        }));

    }

    [Test]
    public async Task WhatHappensIfSecondLevelHandlerThrowsFailFastToo()
    {
        var events = new ConcurrentQueue<string>();

        using var activator = new BuiltinHandlerActivator();

        activator.Handle<string>(async _ =>
        {
            events.Enqueue("1st level handler");
            throw new FailFastException("wooH00 1");
        });

        activator.Handle<IFailed<string>>(async _ =>
        {
            events.Enqueue("2nd level handler");
            throw new FailFastException("wooH00 2");
        });

        var bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new(), "whatever"))
            .Options(o => o.RetryStrategy(secondLevelRetriesEnabled: true))
            .Start();

        await bus.SendLocal("🙂");

        await events.WaitUntil(e => e.Count >= 2);

        Assert.That(events, Is.EqualTo(new[]
        {
            "1st level handler",
            "2nd level handler",
        }));

    }
}