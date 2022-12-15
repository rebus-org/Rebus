using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Retry;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport;
using Rebus.Transport.InMem;
using Exception = System.Exception;
// ReSharper disable ArgumentsStyleLiteral
// ReSharper disable ArgumentsStyleStringLiteral
#pragma warning disable 1998

namespace Rebus.Tests.Extensions;

[TestFixture]
public class ManualDeadlettering : FixtureBase
{
    [Test]
    public async Task CanDeadLetterMessageManuallyWithoutAnyNoise_WithException()
    {
        var listLoggerFactory = new ListLoggerFactory(outputToConsole: true);
        var activator = Using(new BuiltinHandlerActivator());

        activator.Handle<string>(async (bus, message) =>
        {
            await bus.Advanced.TransportMessage.Deadletter(exception: new AbandonedMutexException("why did you abandon me?"));
        });

        var fakeErrorHandler = new FakeErrorHandler();

        Configure.With(activator)
            .Logging(l => l.Use(listLoggerFactory))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "manual-deadlettering"))
            .Options(o => o.Register<IErrorHandler>(c => fakeErrorHandler)) //< provide our own implementation here, so we'll know what gets dead-lettered
            .Start();

        await activator.Bus.SendLocal("HEJ MED DIG MIN VEN");

        var poisonMessage = await fakeErrorHandler.GetNextPoisonMessage(timeoutSeconds: 2);

        Assert.That(poisonMessage.Item2, Is.TypeOf<AbandonedMutexException>());
        Assert.That(poisonMessage.Item2.Message, Is.EqualTo("why did you abandon me?"));

        Console.WriteLine("Exception passed to error handler:");
        Console.WriteLine(poisonMessage.Item2);

        var linesAboveInfo = listLoggerFactory.Where(log => log.Level > LogLevel.Info).ToList();

        if (linesAboveInfo.Any())
        {
            throw new AssertionException($@"Didn't expect NOISE in the log, but the following lines were > INFO:
{string.Join(Environment.NewLine, linesAboveInfo)}");
        }
    }

    [Test]
    public async Task CanDeadLetterMessageManuallyWithoutAnyNoise()
    {
        var listLoggerFactory = new ListLoggerFactory(outputToConsole: true);
        var activator = Using(new BuiltinHandlerActivator());

        activator.Handle<string>(async (bus, _) =>
        {
            await bus.Advanced.TransportMessage.Deadletter(errorDetails: "has been manually dead-lettered");
        });

        var fakeErrorHandler = new FakeErrorHandler();

        Configure.With(activator)
            .Logging(l => l.Use(listLoggerFactory))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "manual-deadlettering"))
            .Options(o => o.Register<IErrorHandler>(_ => fakeErrorHandler)) //< provide our own implementation here, so we'll know what gets dead-lettered
            .Start();

        await activator.Bus.SendLocal("HEJ MED DIG MIN VEN");

        var poisonMessage = await fakeErrorHandler.GetNextPoisonMessage(timeoutSeconds: 2);

        Assert.That(poisonMessage.Item2, Is.TypeOf<RebusApplicationException>());
        Assert.That(poisonMessage.Item2.Message, Is.EqualTo("has been manually dead-lettered"));

        Console.WriteLine("Exception passed to error handler:");
        Console.WriteLine(poisonMessage.Item2);

        var linesAboveInfo = listLoggerFactory.Where(log => log.Level > LogLevel.Info).ToList();

        if (linesAboveInfo.Any())
        {
            throw new AssertionException($@"Didn't expect NOISE in the log, but the following lines were > INFO:

{string.Join(Environment.NewLine, linesAboveInfo)}");
        }
    }

    [Test]
    public async Task CannotDeadletterTwice()
    {
        var caughtExceptions = new ConcurrentQueue<Exception>();

        using var activator = new BuiltinHandlerActivator();

        activator.Handle<string>(async (bus, _) =>
        {
            await bus.Advanced.TransportMessage.Deadletter(errorDetails: "has been manually dead-lettered");

            try
            {
                await bus.Advanced.TransportMessage.Deadletter(errorDetails: "has been manually dead-lettered");
            }
            catch (Exception exception)
            {
                caughtExceptions.Enqueue(exception);
            }
        });

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "manual-deadlettering"))
            .Start();

        await activator.Bus.SendLocal("HEJ MED DIG MIN VEN");

        var caughtException = await caughtExceptions.DequeueNext(timeoutSeconds: 5);

        Console.WriteLine(caughtException);

        Assert.That(caughtException, Is.TypeOf<InvalidOperationException>());
    }

    class FakeErrorHandler : IErrorHandler
    {
        readonly ConcurrentQueue<(TransportMessage, Exception)> _poisonMessages = new();

        public async Task HandlePoisonMessage(TransportMessage transportMessage, ITransactionContext transactionContext, Exception exception) => _poisonMessages.Enqueue((transportMessage, exception));

        public async Task<(TransportMessage, Exception)> GetNextPoisonMessage(int timeoutSeconds = 5) => await _poisonMessages.DequeueNext(timeoutSeconds: timeoutSeconds);
    }
}