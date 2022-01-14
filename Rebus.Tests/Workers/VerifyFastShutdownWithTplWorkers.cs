using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
using Rebus.Workers.TplBased;

namespace Rebus.Tests.Workers;

[TestFixture]
public class VerifyFastShutdownWithTplWorkers : FixtureBase
{
    [Test]
    public async Task CanStopQuicklyWhenCancellationTokenIsUsed()
    {
        var activator = Using(new BuiltinHandlerActivator());
        var messageIsBeingHandled = new ManualResetEvent(false);

        activator.Handle<string>(async (bus, context, str) =>
        {
            messageIsBeingHandled.Set();

            var cancellationToken = context.IncomingStepContext.Load<CancellationToken>();

            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        });

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "quick-quitter"))
            .Options(o => o.UseTplToReceiveMessages())
            .Start();

        await activator.Bus.SendLocal("HEJ MED DIG MIN VEN!");

        messageIsBeingHandled.WaitOrDie(timeout: TimeSpan.FromSeconds(2));

        var stopwatch = Stopwatch.StartNew();

        CleanUpDisposables();

        Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(2)));
    }
}