using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
using Xunit;

namespace Rebus.Tests.Workers
{
    public class TestSetNumberOfWorkers : FixtureBase
    {
        readonly BuiltinHandlerActivator _activator;
        readonly IBus _bus;

        public TestSetNumberOfWorkers()
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

        [Fact]
        public void CanChangeNumberOfWorkersWhileRunning()
        {
            var workers = _bus.Advanced.Workers;

            Assert.Equal(1, workers.Count);

            workers.SetNumberOfWorkers(5);

            Assert.Equal(5, workers.Count);

            workers.SetNumberOfWorkers(1);

            Assert.Equal(1, workers.Count);
        }
    }
}