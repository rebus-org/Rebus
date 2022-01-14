using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Activation;

[TestFixture]
public class RegisterHandlersAfterStartup : FixtureBase
{
    [Test]
    public async Task CannotRegisterNewHandlerAfterStartingBus()
    {
        var activator = new BuiltinHandlerActivator();

        Using(activator);

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "activation"))
            .Start();

        var exception = Assert.Throws<InvalidOperationException>(() => activator.Register(() => new StringHandler()));

        Console.WriteLine(exception);
    }

    [Test]
    public async Task RegisterNewHandlerAfterStartingBus_WithZeroWorkers()
    {
        var activator = new BuiltinHandlerActivator();

        Using(activator);

        var bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "activation"))
            .Start();

        bus.Advanced.Workers.SetNumberOfWorkers(0);

        Assert.DoesNotThrow(() => activator.Register(() => new StringHandler()));

        bus.Advanced.Workers.SetNumberOfWorkers(1);
    }

    [Test]
    public async Task RegisterNewHandlerAfterStartingBus_WithZeroWorkersFromTheGetGo()
    {
        var activator = new BuiltinHandlerActivator();

        Using(activator);

        var bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "activation"))
            .Options(o => o.SetNumberOfWorkers(0))
            .Start();

        Assert.DoesNotThrow(() => activator.Register(() => new StringHandler()));

        bus.Advanced.Workers.SetNumberOfWorkers(1);
    }

    [Test]
    public async Task RegisterNewHandlerAfterStartingBus_WithCreateStartApi()
    {
        var activator = new BuiltinHandlerActivator();

        Using(activator);

        var starter = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "activation"))
            .Create();

        Assert.DoesNotThrow(() => activator.Register(() => new StringHandler()));

        starter.Start();
    }

    class StringHandler : IHandleMessages<string>
    {
        public Task Handle(string message)
        {
            throw new NotImplementedException();
        }
    }
}