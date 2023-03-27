using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Transactions;

[TestFixture, Description("Verifies that Rebus can sensibly handle common stuff in a unit of work")]
public class TestUnitOfWork : FixtureBase
{
    [Test]
    public async Task HandlesExceptionOnCommitAsOrdinaryException()
    {
        var network = new InMemNetwork();

        using var activator = new BuiltinHandlerActivator();

        activator.Handle<string>(async (_, context, _) =>
        {
            var transactionContext = context.TransactionContext;

            transactionContext.OnCommit(async _ => throw new ConcurrencyException());
        });

        var listLoggerFactory = new ListLoggerFactory();

        var bus = Configure.With(activator)
            .Logging(l => l.Use(listLoggerFactory))
            .Transport(t => t.UseInMemoryTransport(network, "uow"))
            .Start();

        await bus.SendLocal("hej!");

        _ = await network.WaitForNextMessageFrom("error");

        var lines = listLoggerFactory.ToList();

        Console.WriteLine("------------------------------------------------------------------------------------------");
        Console.WriteLine(string.Join(Environment.NewLine, lines.Select(line => line.ToString().Limit(200))));
        Console.WriteLine("------------------------------------------------------------------------------------------");

        var warnings = lines.Count(l => l.Level == LogLevel.Warn);
        var errors = lines.Count(l => l.Level == LogLevel.Error);

        Assert.That(warnings, Is.EqualTo(5), "Expected exactly 5 warnings: One for each failed delivery attempt");
        Assert.That(errors, Is.EqualTo(1), "Expected exactly 1 error: One that says that the message is moved to the error queue");
    }

    class ConcurrencyException : Exception { }
}