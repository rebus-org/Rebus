using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Cancellation;

[TestFixture]
public class VerifyLongMessageHandlerCanBeCancelled : FixtureBase
{
    [TestCase(true)]
    [TestCase(false)]
    public async Task ItWorks(bool useExtensionMethod)
    {
        var network = new InMemNetwork();
        var handlerWasEntered = new ManualResetEvent(false);
        var activator = new BuiltinHandlerActivator();

        Using(activator);

        activator.Handle<string>(async (bus, context, message) =>
        {
            var cancellationToken = useExtensionMethod
                ? context.GetCancellationToken()
                : context.IncomingStepContext.Load<CancellationToken>();

            handlerWasEntered.Set();

            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        });

        const string queueName = "cancellation-verification";

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(network, queueName))
            .Start();

        await activator.Bus.SendLocal("HEJ MED DIG");

        // wait until the handler is entered
        handlerWasEntered.WaitOrDie(TimeSpan.FromSeconds(1));

        // wait one more second
        await Task.Delay(TimeSpan.FromSeconds(1));

        // measure how long it takes to stop the bus
        var stopwatch = Stopwatch.StartNew();
        CleanUpDisposables();
        var elapsedDisposingTheBus = stopwatch.Elapsed;

        Assert.That(elapsedDisposingTheBus, Is.LessThan(TimeSpan.FromSeconds(2)), 
            "Expected the bus to have shut down very quickly");

        Assert.That(network.Count(queueName), Is.EqualTo(1), 
            "Expected the message to have been moved back into the input queue");
    }
}