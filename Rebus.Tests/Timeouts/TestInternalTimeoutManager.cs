using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;

#pragma warning disable 1998

namespace Rebus.Tests.Timeouts;

[TestFixture]
public class TestInternalTimeoutManager : FixtureBase
{
    readonly string _queueName = TestConfig.GetName("timeouts");

    [Test]
    public async Task WorksOutOfTheBoxWithInternalTimeoutManager_WhenInMemTimeoutsIsConfigure()
    {
        using (var activator = new BuiltinHandlerActivator())
        {
            var gotTheMessage = new ManualResetEvent(false);

            activator.Handle<string>(async str => gotTheMessage.Set());

            Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), _queueName))
                .Timeouts(t => t.StoreInMemory())
                .Start();

            var stopwatch = Stopwatch.StartNew();

            await activator.Bus.DeferLocal(TimeSpan.FromSeconds(5), "hej med dig min ven!");

            gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(6.5),
                "Message was not received within 6,5 seconds (which it should have been since it was only deferred 5 seconds)");

            Assert.That(stopwatch.Elapsed, Is.GreaterThan(TimeSpan.FromSeconds(5)),
                "It must take more than 5 second to get the message back");
        }
    }
}