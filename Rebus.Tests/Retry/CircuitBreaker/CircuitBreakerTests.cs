using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Retry.CircuitBreaker;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
using System;
using System.Threading.Tasks;

namespace Rebus.Tests.Retry.CircuitBreaker
{
    [TestFixture]
    public class CircuitBreakerTests : FixtureBase
    {
        [Test]
        public async Task Test()
        {
            var network = new InMemNetwork();

            var receiver = Using(new BuiltinHandlerActivator());

            var bus = Configure.With(receiver)
                  .Transport(t => t.UseInMemoryTransport(network, "queue-a"))
                  .Options(o =>
                      {
                          o.SetCircuitBreakers(c => c.OpenOn<MyCustomException>(1, trackingPeriodInSeconds: 10));
                      }
                  )
                  .Start();

            receiver.Handle<string>(async (buss, context, message) =>
            {
                throw new MyCustomException();
            });

            await bus.SendLocal("Uh oh, This is not gonna go well!");

            await Task.Delay(5000);

            var workerCount = bus.Advanced.Workers.Count;
            Assert.That(workerCount, Is.EqualTo(0), $"Expected worker count to be '0' but was {workerCount}");
        }

        class MyCustomException : Exception
        {

        }
    }
}