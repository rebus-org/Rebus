using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Retry.FailFast;
using Rebus.Tests.Contracts;
using Rebus.Transport;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleAnonymousFunction
#pragma warning disable 1998

namespace Rebus.Tests.Assumptions;

[TestFixture]
[Description("This test verifies that it's not possible to mess up Rebus' ambient handler transaction by using a RebusTransactionScope inside the handler")]
public class CheckBehaviorWhenCreatingRebusTransactionScopeInsideRebusHandler : FixtureBase
{
    [Test]
    public async Task CheckIt()
    {
        var activator = Using(new BuiltinHandlerActivator());
        var network = new InMemNetwork();
        var rolledBackMessageReceived = new ManualResetEvent(initialState: false);

        activator.Handle<ThisMessageShouldNotBeSent>(async _ => rolledBackMessageReceived.Set());

        activator.Handle<string>(async (bus, context, message) =>
        {
            using (var scope = new RebusTransactionScope())
            {
                await scope.CompleteAsync();
            }

            await bus.SendLocal(new ThisMessageShouldNotBeSent());

            throw new InvalidOperationException("OH NO!");
        });

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(network, "queue-name"))
            .Options(o => o.FailFastOn<InvalidOperationException>(when: exception => exception.Message.Contains("OH NO!")))
            .Start();

        await activator.Bus.SendLocal("HEJ MED DIG MIN VEN");

        Assert.That(rolledBackMessageReceived.WaitOne(TimeSpan.FromSeconds(3)), Is.False,
            "Did not expect to receive the ThisMessageShouldNotBeSent, because its Rebus handler transaction was rolled back by an exception");
    }

    class ThisMessageShouldNotBeSent { }
}