using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestShutdownWithPendingTasks : FixtureBase
{
    [Test]
    public async Task DoIt()
    {
        var builtinHandlerActivator = new BuiltinHandlerActivator();
        var allDone = false;
        var gotMessage = new ManualResetEvent(false);

        builtinHandlerActivator.Handle<string>(async _ =>
        {
            gotMessage.Set();

            await Task.Delay(2000);

            allDone = true;
        });

        var bus = Configure.With(builtinHandlerActivator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "shutdown with pending tasks"))
            .Start();

        using (bus)
        {
            await bus.SendLocal("hej");

            gotMessage.WaitOrDie(TimeSpan.FromSeconds(2));

            // make bus shut down here
        }

        Assert.That(allDone, Is.True, "The message was apparently not handled all the way to the end!!!");
    }
}