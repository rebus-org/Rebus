using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Examples;

[TestFixture]
public class TheMostInterestingHandlerInTheWorldWorks : FixtureBase
{
    [Test]
    public async Task OfCourseHandlerCanBeSimpleLikeThis()
    {
        var logs = new ListLoggerFactory();

        using var activator = new BuiltinHandlerActivator();

        activator.Register(() => new TheMostInterestingHandlerInTheWorld());

        var bus = Configure.With(activator)
            .Logging(l => l.Use(logs))
            .Transport(t => t.UseInMemoryTransport(new(), "whatever"))
            .Start();

        await bus.SendLocal(new MyMessage());

        // wait a short while
        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.That(logs.Select(line => line.Text), Does.Not.Contain("InvalidOperationException"));
    }

    record MyMessage;

    class TheMostInterestingHandlerInTheWorld : IHandleMessages<MyMessage>
    {
        public Task Handle(MyMessage message)
        {
            Console.WriteLine("Receive my message");
            return Task.CompletedTask;
        }
    }
}