using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests;
using Rebus.Transport.InMem;

namespace Rebus.Async.Tests
{
    [TestFixture]
    public class CanRegisterInlineHandler : FixtureBase
    {
        const string InputQueueName = "inline-handlers";
        IBus _bus;
        BuiltinHandlerActivator _activator;

        protected override void SetUp()
        {
            _activator = new BuiltinHandlerActivator();

            Using(_activator);

            _bus = Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), InputQueueName))
                .Options(o => o.EnableSynchronousRequestReply())
                .Routing(r => r.TypeBased().Map<SomeRequest>(InputQueueName))
                .Start();
        }

        [Test]
        public async Task NizzleName()
        {
            _activator.Handle<SomeRequest>(async (bus, request) =>
            {
                await bus.Reply(new SomeReply());
            });

            var reply = await _bus.SendRequest<SomeReply>(new SomeRequest());

            Assert.That(reply, Is.Not.Null);
        }

        public class SomeRequest { }
        public class SomeReply { }
    }
}
