using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Workers;

[TestFixture]
public class TestSetNumberOfWorkers : FixtureBase
{
    BuiltinHandlerActivator _activator;
    IBus _bus;

    protected override void SetUp()
    {
        _activator = new BuiltinHandlerActivator();

        Using(_activator);

        Configure.With(_activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "set-number-of-workers"))
            .Options(o =>
            {
                o.SetMaxParallelism(10);
                o.SetNumberOfWorkers(1);
            })
            .Start();

        _bus = _activator.Bus;
    }

    [Test]
    public void CanChangeNumberOfWorkersWhileRunning()
    {
        var workers = _bus.Advanced.Workers;

        Assert.That(workers.Count, Is.EqualTo(1));

        workers.SetNumberOfWorkers(5);

        Assert.That(workers.Count, Is.EqualTo(5));

        workers.SetNumberOfWorkers(1);

        Assert.That(workers.Count, Is.EqualTo(1));
    }
}