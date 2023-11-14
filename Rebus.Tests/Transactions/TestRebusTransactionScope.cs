using System;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Transactions;

[TestFixture]
public class TestRebusTransactionScope : FixtureBase
{
    [Test]
    public void ThrowsInvalidOperationExceptionWhenTryingToEnlistOperationsFromMultipleBusInstancesInTheSameScope()
    {
        IBus CreateBusInstance() => Configure.With(new BuiltinHandlerActivator())
            .Transport(t => t.UseInMemoryTransport(new(), "whatever"))
            .Start();

        using var bus1 = CreateBusInstance();
        using var bus2 = CreateBusInstance();

        using var scope = new RebusTransactionScope();

        // this is ok
        bus1.Advanced.SyncBus.SendLocal("HEJ MED DIG");

        // this must throw
        var exception = Assert.Throws<InvalidOperationException>(() => bus2.Advanced.SyncBus.SendLocal("CANNOT DO THAT"));

        Console.WriteLine(exception);
    }
}